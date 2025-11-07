namespace SsasMcpServer.Models.Query;

public class QueryFilter
{
    public string Column { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
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