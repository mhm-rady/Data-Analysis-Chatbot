using System.Data;
using System.Diagnostics;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SsasMcpServer.Models.Configuration;
using SsasMcpServer.Models.Exceptions;
using SsasMcpServer.Models.Query;
using SsasMcpServer.Services.Interfaces;

namespace SsasMcpServer.Services.Implementations;

public class QueryService : IQueryService
{
    private readonly ILogger<QueryService> _logger;
    private readonly ISsasConnectionService _connectionService;
    private readonly IDaxQueryBuilder _queryBuilder;
    private readonly IMetadataService _metadataService;
    private readonly McpServerOptions _options;

    public QueryService(
        ILogger<QueryService> logger,
        ISsasConnectionService connectionService,
        IDaxQueryBuilder queryBuilder,
        IMetadataService metadataService,
        IOptions<McpServerOptions> options)
    {
        _logger = logger;
        _connectionService = connectionService;
        _queryBuilder = queryBuilder;
        _metadataService = metadataService;
        _options = options.Value;
    }

    public async Task<QueryResult> ExecuteQueryAsync(
        QueryRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing query with {ColumnCount} columns and {MeasureCount} measures", 
            request.Columns.Count, request.Measures.Count);

        // Validate request
        var (isValid, errors) = await ValidateQueryRequestAsync(request, cancellationToken);
        if (!isValid)
        {
            var errorMsg = string.Join("; ", errors);
            _logger.LogError("Query validation failed: {Errors}", errorMsg);
            throw new QueryExecutionException($"Query validation failed: {errorMsg}");
        }

        // Apply max rows limit
        var maxRows = Math.Min(request.MaxRows, _options.MaxResultRows);
        request.MaxRows = maxRows;

        // Generate DAX query
        var daxQuery = await GenerateDaxQueryAsync(request, cancellationToken);

        // Execute query
        return await ExecuteDaxQueryAsync(daxQuery, maxRows, cancellationToken);
    }

    public async Task<string> GenerateDaxQueryAsync(
        QueryRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating DAX query from request");

        try
        {
            var daxQuery = _queryBuilder.BuildQuery(request);
            _logger.LogDebug("Generated DAX query: {Query}", daxQuery);
            return await Task.FromResult(daxQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate DAX query");
            throw new QueryExecutionException("Failed to generate DAX query", ex);
        }
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateQueryRequestAsync(
        QueryRequest request, 
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Basic validation
        if (request.Columns.Count == 0 && request.Measures.Count == 0)
        {
            errors.Add("At least one column or measure must be specified");
        }

        if (request.MaxRows <= 0)
        {
            errors.Add("MaxRows must be greater than 0");
        }

        if (request.MaxRows > _options.MaxResultRows)
        {
            errors.Add($"MaxRows cannot exceed {_options.MaxResultRows}");
        }

        // Validate columns exist in metadata
        try
        {
            var metadata = await _metadataService.GetModelMetadataAsync(request.Culture, cancellationToken);

            // Validate columns
            foreach (var column in request.Columns)
            {
                if (!IsValidColumn(column, metadata))
                {
                    errors.Add($"Column '{column}' not found in model metadata");
                }
            }

            // Validate measures
            foreach (var measure in request.Measures)
            {
                if (!IsValidMeasure(measure, metadata))
                {
                    errors.Add($"Measure '{measure}' not found in model metadata");
                }
            }

            // Validate filter columns
            foreach (var filter in request.Filters)
            {
                if (!IsValidColumn(filter.Column, metadata))
                {
                    errors.Add($"Filter column '{filter.Column}' not found in model metadata");
                }
            }

            // Validate sort columns
            foreach (var sort in request.Sorts)
            {
                var isValidSortColumn = IsValidColumn(sort.Column, metadata) || 
                                       IsValidMeasure(sort.Column, metadata);
                if (!isValidSortColumn)
                {
                    errors.Add($"Sort column '{sort.Column}' not found in model metadata");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not validate against metadata");
            errors.Add("Could not validate query against model metadata");
        }

        return (errors.Count == 0, errors);
    }

    public async Task<QueryResult> ExecuteDaxQueryAsync(
        string daxQuery, 
        int maxRows = 1000, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing DAX query");
        
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryResult { DaxQuery = daxQuery };

        try
        {
            var connection = await _connectionService.GetAdomdConnectionAsync(cancellationToken);
            
            using var command = new AdomdCommand(daxQuery, connection);
            command.CommandTimeout = 300; // 5 minutes timeout

            using var reader = command.ExecuteReader();
            
            // Extract column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.ColumnNames.Add(reader.GetName(i));
            }

            // Read data rows
            int rowCount = 0;
            while (reader.Read())
            {
                if (rowCount >= maxRows)
                {
                    result.IsTruncated = true;
                    _logger.LogWarning("Result set truncated at {MaxRows} rows", maxRows);
                    break;
                }

                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[result.ColumnNames[i]] = value;
                }

                result.Rows.Add(row);
                rowCount++;
            }

            result.TotalRows = rowCount;
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            _logger.LogInformation("Query executed successfully. Rows: {RowCount}, Duration: {Duration}ms", 
                result.TotalRows, result.ExecutionTime.TotalMilliseconds);

            return result;
        }
        catch (AdomdException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "ADOMD query execution failed");
            throw new QueryExecutionException($"Query execution failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query execution failed");
            throw new QueryExecutionException("Query execution failed", ex);
        }
    }

    private bool IsValidColumn(string column, Models.Metadata.TabularModelMetadata metadata)
    {
        // Extract table and column names
        var (tableName, columnName) = ParseColumnReference(column);

        foreach (var dimension in metadata.Dimensions)
        {
            // Match table name
            if (!string.IsNullOrEmpty(tableName) && 
                !dimension.TechnicalName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                continue;

            // Match column name
            if (dimension.Attributes.Any(a => 
                a.TechnicalName.Equals(columnName, StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidMeasure(string measure, Models.Metadata.TabularModelMetadata metadata)
    {
        // Extract measure name from various formats
        var measureName = ExtractMeasureName(measure);

        return metadata.Measures.Any(m =>
            m.TechnicalName.Equals(measure, StringComparison.OrdinalIgnoreCase) ||
            m.DisplayName.Equals(measureName, StringComparison.OrdinalIgnoreCase) ||
            m.TechnicalName.Contains($"[{measureName}]", StringComparison.OrdinalIgnoreCase));
    }

    private (string TableName, string ColumnName) ParseColumnReference(string column)
    {
        // Handle Table[Column] format
        if (column.Contains('[') && column.Contains(']'))
        {
            var idx = column.IndexOf('[');
            var tableName = column.Substring(0, idx).Trim().Trim('\'');
            var columnName = column.Substring(idx + 1, column.IndexOf(']') - idx - 1);
            return (tableName, columnName);
        }

        // Handle Table.Column format
        if (column.Contains('.'))
        {
            var parts = column.Split('.');
            return (parts[0].Trim(), parts[1].Trim());
        }

        // No table specified
        return (string.Empty, column);
    }

    private string ExtractMeasureName(string measure)
    {
        // Handle Table[Measure] format
        if (measure.Contains('[') && measure.Contains(']'))
        {
            var start = measure.IndexOf('[') + 1;
            var end = measure.IndexOf(']');
            return measure.Substring(start, end - start);
        }

        return measure;
    }
}