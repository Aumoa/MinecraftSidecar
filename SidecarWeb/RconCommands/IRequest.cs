namespace SidecarWeb.RconCommands;

internal interface IRequest
{
    ValueTask<IResponse> ParseAsync(string commandResult, CancellationToken cancellationToken = default);
}

internal interface IRequest<TResponse> : IRequest where TResponse : IResponse
{
    new ValueTask<TResponse> ParseAsync(string commandResult, CancellationToken cancellationToken = default);

    async ValueTask<IResponse> IRequest.ParseAsync(string commandResult, CancellationToken cancellationToken)
    {
        return await ParseAsync(commandResult, cancellationToken);
    }
}