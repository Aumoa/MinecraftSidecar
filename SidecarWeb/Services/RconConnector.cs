using System.Net;
using CoreRCON;
using Microsoft.Extensions.Options;
using SidecarWeb.Options;
using SidecarWeb.RconCommands;

namespace SidecarWeb.Services;

public class RconConnector(ILogger<RconConnector> logger, IOptions<RconConnectorOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            using var rcon = new RCON(IPEndPoint.Parse(options.Value.Server), options.Value.Secret, logger: logger);

            try
            {
                await rcon.ConnectAsync().WaitAsync(stoppingToken);
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
                continue;
            }
        }
    }
}
