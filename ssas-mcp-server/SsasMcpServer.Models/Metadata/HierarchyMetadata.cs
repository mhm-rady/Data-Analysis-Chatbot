namespace SsasMcpServer.Models.Metadata;

public class HierarchyMetadata
{
    public string TechnicalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CultureInfo> Cultures { get; set; } = new();
    public List<HierarchyLevelMetadata> Levels { get; set; } = new();
    public bool IsHidden { get; set; }
}