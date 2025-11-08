using System.ComponentModel;

namespace SsasMcpServer.Models.Query;

public class QueryFilter
{
    [Description("The column technical name to filter on.")]
    public string Column { get; set; } = string.Empty;
    [Description("Filter operator: Equals, NotEquals, GreaterThan, LessThan, Contains, StartsWith, In, etc.")]
    public FilterOperator Operator { get; set; }
    [Description("Filter value (string, number, or array for 'In' operator)")]
    public object Value { get; set; } = null!;
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEquals,
    LessThan,
    LessThanOrEquals,
    Contains,
    StartsWith,
    EndsWith,
    In,
    NotIn
}