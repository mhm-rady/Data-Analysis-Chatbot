namespace SsasMcpServer.Models.Metadata;

public class AttributeMetadata
{
    public string TechnicalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CultureInfo> Cultures { get; set; } = new();
    public string DataType { get; set; } = string.Empty;
    public bool IsKey { get; set; }
    public bool IsHidden { get; set; }
    public string? FormatString { get; set; }
}