namespace SsasMcpServer.Models.Query;

public class QueryResult
{
    public List<string> ColumnNames { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public bool IsTruncated { get; set; }
    public string? DaxQuery { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}