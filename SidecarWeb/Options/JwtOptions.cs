namespace SidecarWeb.Options;

public record JwtOptions
{
    public required string Salt { get; set; }
}
