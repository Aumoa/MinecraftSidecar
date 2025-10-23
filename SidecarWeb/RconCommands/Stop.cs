namespace SidecarWeb.RconCommands;

internal static partial class Stop
{
    public record Response() : IResponse
    {
        public override string ToString()
        {
            return "Stopping...";
        }
    }

    public record Request() : IRequest<Response>
    {
        public override string ToString()
        {
            return "stop";
        }

        public ValueTask<Response> ParseAsync(string commandResult, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new Response());
        }
    }

    public static Request Get()
    {
        return new Request();
    }
}