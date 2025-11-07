using SsasMcpServer.Models.Query;

namespace SsasMcpServer.Services.Interfaces;

/// <summary>
/// Builder service for constructing DAX queries
/// </summary>
public interface IDaxQueryBuilder
{
    /// <summary>
    /// Builds a DAX query from query request
    /// </summary>
    string BuildQuery(QueryRequest request);
    
    /// <summary>
    /// Builds filter expression for DAX
    /// </summary>
    string BuildFilterExpression(QueryFilter filter);
    
    /// <summary>
    /// Builds ORDER BY clause for DAX
    /// </summary>
    string BuildOrderByClause(List<QuerySort> sorts);
    
    /// <summary>
    /// Escapes column/table names for DAX
    /// </summary>
    string EscapeDaxIdentifier(string identifier);
    
    /// <summary>
    /// Formats values for DAX expressions
    /// </summary>
    string FormatDaxValue(object value, FilterOperator op);
}