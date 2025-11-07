namespace SsasMcpServer.Models.Query;

public class QueryRequest
{
    public List<string> Columns { get; set; } = new();
    public List<string> Measures { get; set; } = new();
    public List<QueryFilter> Filters { get; set; } = new();
    public List<QuerySort> Sorts { get; set; } = new();
    public int? TopN { get; set; }
    public int MaxRows { get; set; } = 1000;
    public string? Culture { get; set; }
}

