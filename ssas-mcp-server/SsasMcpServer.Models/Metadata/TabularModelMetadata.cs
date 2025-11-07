namespace SsasMcpServer.Models.Metadata;

public class TabularModelMetadata
{
    public string DatabaseName { get; set; } = string.Empty;
    public string? ModelDescription { get; set; }
    public List<string> SupportedCultures { get; set; } = new();
    public List<DimensionMetadata> Dimensions { get; set; } = new();
    public List<MeasureMetadata> Measures { get; set; } = new();
    public List<KpiMetadata> Kpis { get; set; } = new();
    public DateTime LastProcessed { get; set; }
    public string CompatibilityLevel { get; set; } = string.Empty;
}
