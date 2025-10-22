using System.Net;
using System.Text.RegularExpressions;
using CoreRCON;
using Microsoft.Extensions.Options;
using SidecarWeb.Options;
using SidecarWeb.RconCommands;

namespace SidecarWeb.Services;

public partial class RconConnector(ILogger<RconConnector> logger, IOptions<RconConnectorOptions> options) : BackgroundService
{
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
                    rcon.Dispose();
                    continue;
                }
                finally
                {
                }

                try
                {
                    while (true)
                    {
                        string result = await rcon.SendCommandAsync("list").WaitAsync(stoppingToken);
                        var response = await Players.Get().ParseAsync(result, stoppingToken);
                        logger.LogInformation("list result: {result}", response);
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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
