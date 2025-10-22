namespace SidecarWeb.RconCommands;

internal interface IRequest
{
    ValueTask<IResponse> ParseAsync(string commandResult, CancellationToken cancellationToken = default);
}
