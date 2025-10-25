using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CoreRCON;
using Microsoft.Extensions.Options;
using SidecarWeb.Options;
using SidecarWeb.RconCommands;

namespace SidecarWeb.Services;

internal partial class RconConnector(ILogger<RconConnector> logger, IOptions<RconConnectorOptions> options) : BackgroundService
{
    private class RequestPromise(IRequest request)
    {
        private readonly TaskCompletionSource<IResponse> m_Tcs = new();

        public readonly IRequest Request = request;
        public Task<IResponse> ResponseTask => m_Tcs.Task;

        public void SetResult(IResponse response)
        {
            m_Tcs.SetResult(response);
        }

        public void SetCanceled()
        {
            m_Tcs.SetCanceled();
        }

        public void SetException(Exception exception)
        {
            m_Tcs.SetException(exception);
        }
    }

    private readonly Channel<RequestPromise> m_CommandChannel = Channel.CreateUnbounded<RequestPromise>();

    public async ValueTask<TResponse> SendCommandAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        where TResponse : IResponse
    {
        var promise = new RequestPromise(request);
        await m_CommandChannel.Writer.WriteAsync(promise, cancellationToken);
        return (TResponse)await promise.ResponseTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            foreach (var addr in await ResolveEndPointAsync(options.Value.Server, stoppingToken))
            {
                var rcon = new RCON(addr, options.Value.Secret, logger: logger);
                try
                {
                    await rcon.ConnectAsync().WaitAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogInformation("RCON connection failed: {Message}", e.Message);
                    continue;
                }

                try
                {
                    var reader = m_CommandChannel.Reader;
                    while (true)
                    {
                        var promise = await reader.ReadAsync(stoppingToken);
                        var command = promise.Request.ToString();
                        string result = await rcon.SendCommandAsync(command).WaitAsync(stoppingToken);
                        _ = promise.Request.ParseAsync(result, stoppingToken).AsTask().ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully)
                            {
                                promise.SetResult(t.Result);
                            }
                            else if (t.IsCanceled)
                            {
                                promise.SetCanceled();
                            }
                            else
                            {
                                promise.SetException(t.Exception!);
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    logger.LogInformation("RCON connection failed: {Message}", e.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    break;
                }
                finally
                {
                    rcon.Dispose();
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private static async ValueTask<IEnumerable<IPEndPoint>> ResolveEndPointAsync(string endpoint, CancellationToken cancellationToken)
    {
        var match = HostAndPortRegex().Match(endpoint);
        if (!match.Success)
        {
            throw new FormatException("Invalid endpoint format");
        }

        string host = match.Groups[1].Value;
        int port = int.Parse(match.Groups[2].Value);

        if (IPv4Regex().Match(host).Success)
        {
            return [IPEndPoint.Parse(endpoint)];
        }

        IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0)
        {
            throw new InvalidOperationException("Could not resolve host");
        }

        return addresses.Select(a => new IPEndPoint(a, port));
    }

    [GeneratedRegex(@"^([^:]+)(?::(\d+))?$")]
    private static partial Regex HostAndPortRegex();

    [GeneratedRegex(@"^(?:25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)){3}$")]
    private static partial Regex IPv4Regex();
}
