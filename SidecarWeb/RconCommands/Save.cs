namespace SidecarWeb.RconCommands;

internal static partial class Save
{
    public record Response() : IResponse
    {
        public override string ToString()
        {
            return "Save completed.";
        }
    }

    public record Request() : IRequest<Response>
    {
        public override string ToString()
        {
            return "save-all";
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