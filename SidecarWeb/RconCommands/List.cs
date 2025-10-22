using System.Text.RegularExpressions;

namespace SidecarWeb.RconCommands;

internal static partial class Players
{
    public record Response(string[] Users, int MaxPlayers) : IResponse
    {
        public override string ToString()
        {
            return $"There are {Users.Length} of a max of {MaxPlayers} players online: {string.Join(", ", Users)}";
        }
    }

    public partial record Request() : IRequest
    {
        public ValueTask<IResponse> ParseAsync(string commandResult, CancellationToken cancellationToken)
        {
            var match = ResultRegex().Match(commandResult);
            if (match.Success == false)
            {
                throw new InvalidOperationException();
            }

            var remains = commandResult[match.Value.Length..];
            var list = remains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int maxPlayers = int.Parse(match.Groups[1].Value);

            return ValueTask.FromResult<IResponse>(new Response([.. list], maxPlayers));
        }

        [GeneratedRegex(@"There are \d+ of a max of (\d+) players online:")]
        private static partial Regex ResultRegex();
    }

    public static Request Get()
    {
        return new Request();
    }
}
