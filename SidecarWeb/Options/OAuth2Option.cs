namespace SidecarWeb.Options;

public record OAuth2Option
{
    public required string Authorize { get; set; }

    public required string Token { get; set; }

    public required string UserInfo { get; set; }

    public required string ClientId { get; set; }
}
