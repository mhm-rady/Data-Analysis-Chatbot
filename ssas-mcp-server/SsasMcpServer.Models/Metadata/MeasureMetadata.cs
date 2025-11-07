namespace SsasMcpServer.Models.Metadata;

public class MeasureMetadata
{
    public string TechnicalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CultureInfo> Cultures { get; set; } = new();
    public string? FormatString { get; set; }
    public string? Expression { get; set; }
    public bool IsHidden { get; set; }
    public string? DisplayFolder { get; set; }
}
