namespace SidecarWeb.Options;

public record RconConnectorOptions
{
    public required string Server { get; set; }
    
    public required string Secret { get; set; }
}
