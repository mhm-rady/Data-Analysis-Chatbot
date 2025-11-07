using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;

namespace SsasMcpServer.Services.Interfaces;

/// <summary>
/// Service for managing SSAS connections and server operations
/// </summary>
public interface ISsasConnectionService : IDisposable
{
    /// <summary>
    /// Gets an ADOMD connection for query execution
    /// </summary>
    Task<AdomdConnection> GetAdomdConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a Tabular connection for metadata operations
    /// </summary>
    Task<Server> GetTabularServerAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to SSAS server
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current database name
    /// </summary>
    string GetDatabaseName();
}