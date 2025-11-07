using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SsasMcpServer.Models.Configuration;
using SsasMcpServer.Models.Exceptions;
using SsasMcpServer.Services.Interfaces;

namespace SsasMcpServer.Services.Implementations;

public class SsasConnectionService : ISsasConnectionService
{
    private readonly ILogger<SsasConnectionService> _logger;
    private readonly SsasConnectionOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private AdomdConnection? _adomdConnection;
    private Server? _tabularServer;
    private bool _disposed;

    public SsasConnectionService(
        ILogger<SsasConnectionService> logger,
        IOptions<SsasConnectionOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        ValidateOptions();
    }

    public async Task<AdomdConnection> GetAdomdConnectionAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_adomdConnection == null || _adomdConnection.State != System.Data.ConnectionState.Open)
            {
                _logger.LogInformation("Creating new ADOMD connection to {Server}", _options.Server);
                
                _adomdConnection?.Dispose();
                _adomdConnection = new AdomdConnection(_options.BuildConnectionString());
                
                _adomdConnection.Open();
                
                _logger.LogInformation("ADOMD connection established successfully");
            }
            
            return _adomdConnection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish ADOMD connection");
            throw new SsasException("Failed to connect to SSAS server", ex);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<Server> GetTabularServerAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_tabularServer == null || !_tabularServer.Connected)
            {
                _logger.LogInformation("Creating new Tabular server connection to {Server}", _options.Server);
                
                _tabularServer?.Dispose();
                _tabularServer = new Server();
                
                //await Task.Run(() => _tabularServer.Connect(_options.BuildConnectionString()), cancellationToken);
                _tabularServer.Connect("Data Source=localhost;Catalog=Adventure Works Internet Sales;");
                _logger.LogInformation("Tabular server connection established successfully");
            }
            
            return _tabularServer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish Tabular server connection");
            throw new SsasException("Failed to connect to SSAS Tabular server", ex);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to SSAS server");
            
            var connection = await GetAdomdConnectionAsync(cancellationToken);
            var server = await GetTabularServerAsync(cancellationToken);
            
            var database = server.Databases.FindByName(_options.Database);
            if (database == null)
            {
                _logger.LogError("Database {Database} not found on server", _options.Database);
                return false;
            }
            
            _logger.LogInformation("Connection test successful. Database: {Database}, Compatibility: {Level}", 
                database.Name, database.CompatibilityLevel);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    public string GetDatabaseName() => _options.Database;

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Server))
            throw new ArgumentException("SSAS Server must be specified", nameof(_options.Server));
        
        if (string.IsNullOrWhiteSpace(_options.Database))
            throw new ArgumentException("SSAS Database must be specified", nameof(_options.Database));
        
        if (!_options.UseWindowsAuth && string.IsNullOrWhiteSpace(_options.Username))
            throw new ArgumentException("Username must be specified when not using Windows Authentication");
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _adomdConnection?.Dispose();
        _tabularServer?.Dispose();
        _connectionLock?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}