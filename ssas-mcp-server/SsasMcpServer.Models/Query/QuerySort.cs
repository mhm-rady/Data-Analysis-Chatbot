using System.ComponentModel;

namespace SsasMcpServer.Models.Query;

public class QuerySort
{
    [Description("The column technical name to sort by.")]
    public string Column { get; set; } = string.Empty;
    [Description("Sort direction: Ascending or Descending.")]
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public enum SortDirection
{
    Ascending,
    Descending
}