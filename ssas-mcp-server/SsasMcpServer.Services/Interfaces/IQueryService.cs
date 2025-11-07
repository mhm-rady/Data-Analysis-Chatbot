using SsasMcpServer.Models.Query;

namespace SsasMcpServer.Services.Interfaces;

/// <summary>
/// Service for executing queries against SSAS tabular model
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Executes a query based on the request parameters
    /// </summary>
    Task<QueryResult> ExecuteQueryAsync(
        QueryRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates DAX query from request without executing
    /// </summary>
    Task<string> GenerateDaxQueryAsync(
        QueryRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a query request before execution
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateQueryRequestAsync(
        QueryRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a pre-validated DAX query directly
    /// </summary>
    Task<QueryResult> ExecuteDaxQueryAsync(
        string daxQuery, 
        int maxRows = 1000, 
        CancellationToken cancellationToken = default);
}