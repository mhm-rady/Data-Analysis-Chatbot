namespace SsasMcpServer.Models.Metadata;

public class DimensionMetadata
{
    public string TechnicalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CultureInfo> Cultures { get; set; } = new();
    public List<AttributeMetadata> Attributes { get; set; } = new();
    public List<HierarchyMetadata> Hierarchies { get; set; } = new();
    public bool IsHidden { get; set; }
}