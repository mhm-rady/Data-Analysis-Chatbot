namespace SsasMcpServer.Models.Metadata;

public class KpiMetadata
{
    public string TechnicalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CultureInfo> Cultures { get; set; } = new();
    public string? ValueExpression { get; set; }
    public string? GoalExpression { get; set; }
    public string? StatusExpression { get; set; }
    public string? TrendExpression { get; set; }
    public string? DisplayFolder { get; set; }
}