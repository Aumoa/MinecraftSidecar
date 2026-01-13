namespace MinecraftSidecar.Options;

public record OIDC
{
    public required string Uri { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
