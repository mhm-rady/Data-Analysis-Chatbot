namespace SsasMcpServer.Models.Metadata;

public class HierarchyLevelMetadata
{
    public string TechnicalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SourceAttribute { get; set; } = string.Empty;
    public int Ordinal { get; set; }
}