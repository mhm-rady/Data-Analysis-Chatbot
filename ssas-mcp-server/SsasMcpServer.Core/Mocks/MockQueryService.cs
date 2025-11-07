using System.Diagnostics;
using SsasMcpServer.Models.Query;
using SsasMcpServer.Services.Interfaces;

namespace SsasMcpServer.Core.Mocks;

/// <summary>
/// Lightweight mock IQueryService for local demos and tests. Does not contact SSAS.
/// </summary>
public class MockQueryService : IQueryService
{
    public Task<QueryResult> ExecuteQueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        // Create a simple mock result that matches requested columns/measures
        var result = new QueryResult
        {
            DaxQuery = "// MOCK: generated DAX",
            ExecutionTime = TimeSpan.FromMilliseconds(5)
        };

        // Columns: include requested columns and measures as column names
        foreach (var col in request.Columns)
            result.ColumnNames.Add(col);
        foreach (var m in request.Measures)
            result.ColumnNames.Add(m);

        // Create some mock rows (up to MaxRows or 3 rows)
        var rowsToCreate = Math.Min(request.MaxRows, 3);
        for (int i = 0; i < rowsToCreate; i++)
        {
            var row = new Dictionary<string, object?>();
            foreach (var col in result.ColumnNames)
            {
                if (col == request.Measures.FirstOrDefault())
                    row[col] = 1000 * (i + 1); // mock measure values
                else
                    row[col] = $"Value_{i + 1}";
            }

            result.Rows.Add(row);
        }

        result.TotalRows = result.Rows.Count;
        result.IsTruncated = result.TotalRows < request.MaxRows ? false : false;

        return Task.FromResult(result);
    }

    public Task<string> GenerateDaxQueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("// MOCK: DAX for request");
    }

    public Task<(bool IsValid, List<string> Errors)> ValidateQueryRequestAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        // Very basic validation for demo
        var errors = new List<string>();
        if ((request.Columns == null || request.Columns.Count == 0) && (request.Measures == null || request.Measures.Count == 0))
            errors.Add("At least one column or measure must be specified");

        if (request.MaxRows <= 0)
            errors.Add("MaxRows must be greater than 0");

        return Task.FromResult((errors.Count == 0, errors));
    }

    public Task<QueryResult> ExecuteDaxQueryAsync(string daxQuery, int maxRows = 1000, CancellationToken cancellationToken = default)
    {
        // Return a simple mock result for a raw DAX query
        var result = new QueryResult
        {
            DaxQuery = daxQuery,
            ExecutionTime = TimeSpan.FromMilliseconds(3),
            TotalRows = 0
        };

        return Task.FromResult(result);
    }
}
