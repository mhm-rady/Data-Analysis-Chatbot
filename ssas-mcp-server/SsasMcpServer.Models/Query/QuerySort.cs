namespace SsasMcpServer.Models.Query;

public class QuerySort
{
    public string Column { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public enum SortDirection
{
    Ascending,
    Descending
}