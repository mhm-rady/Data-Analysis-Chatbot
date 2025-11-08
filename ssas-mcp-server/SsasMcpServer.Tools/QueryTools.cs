
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SsasMcpServer.Models.Query;
using SsasMcpServer.Services.Interfaces;
using System.ComponentModel;
using System.Text.Json;

namespace SsasMcpServer.Tools;

[McpServerToolType]
public sealed class QueryTools
{

    [McpServerTool, Description("Execute a data query against the SSAS model with specified columns, measures, filters, sorts, and options.")]
    public static async Task<string> ValidateQuery(
        IQueryService queryService,
        ILogger<QueryTools> logger,
        [Description("List of dimension attribute technical names (e.g., ['DimProduct'[ProductName], 'DimDate'[CalendarYear]])")] string[] columns,
        [Description("List of measure technical names (e.g., ['Measures'[TotalSales], 'Measures'[TotalQuantity]])")] string[] measures,
        [Description("List of optional filters to apply")] QueryFilter[] filters,
        [Description("Optional culture code for query execution")] string? culture,
        CancellationToken cancellationToken
    )
    {
        try
        {
            logger.LogInformation("Validating query with {Columns} columns, {Measures} measures, culture: {Culture}",
                columns.Length, measures.Length, culture ?? "default");

            QueryRequest request = new()
            {
                Columns = [.. columns],
                Measures = [.. measures],
                Filters = [.. filters],
                Culture = culture
            };

            var (isValid, errors) = await queryService.ValidateQueryRequestAsync(request, cancellationToken);

            return JsonSerializer.Serialize(new
            {
                isValid,
                errors,
                message = isValid ? "Query request is valid" : "Query request has validation errors"
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating query");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Execute an analytical query against SSAS tabular model. Constructs and executes DAX query internally. Returns result rows with requested columns and measures.")]
    public static async Task<string> ExecuteQuery(
        IQueryService queryService,
        ILogger<QueryTools> logger,
        [Description("List of dimension attribute technical names (e.g., ['DimProduct[ProductName]', 'DimDate[CalendarYear]'])")] string[] columns,
        [Description("List of measure technical names (e.g., ['Measures[TotalSales]', 'Measures[TotalQuantity]'])")] string[] measures,
        [Description("Optional list of filters to apply")] QueryFilter[] filters,
        [Description("Optional list of sort options to apply")] QuerySort[] sorts,
        [Description("Optional: return only top N rows (applied after sorting)")] int? topN,
        [Description("Maximum number of rows to return")] int? maxRows,
        [Description("Optional culture code for query execution")] string? culture,
        CancellationToken cancellationToken
    )
    {
        try
        {
            logger.LogInformation("Executing query with {Columns} columns, {Measures} measures, culture: {Culture}, maxRows: {MaxRows}",
                columns.Length, measures.Length, culture ?? "default", maxRows);

            QueryRequest request = new()
            {
                Columns = [.. columns],
                Measures = [.. measures],
                Filters = [.. filters],
                Sorts = [.. sorts],
                TopN = topN,
                MaxRows = maxRows ?? 1000,
                Culture = culture,
            };

            var result = await queryService.ExecuteQueryAsync(request, cancellationToken);

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing query");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Generate DAX query text from query parameters without executing it. Useful for understanding or validating the query that would be executed.")]
    public static async Task<string> GenerateDaxQuery(
        IQueryService queryService,
        ILogger<QueryTools> logger,
        [Description("List of dimension attribute technical names (e.g., ['DimProduct[ProductName]', 'DimDate[CalendarYear]'])")] string[] columns,
        [Description("List of measure technical names (e.g., ['Measures[TotalSales]', 'Measures[TotalQuantity]'])")] string[] measures,
        [Description("Optional list of filters to apply")] QueryFilter[] filters,
        [Description("Optional list of sort options to apply")] QuerySort[] sorts,
        [Description("Optional culture code for query generation")] string? culture,
        CancellationToken cancellationToken
    )
    {
        try
        {
            logger.LogInformation("Generating DAX query with {Columns} columns, {Measures} measures, culture: {Culture}",
                columns.Length, measures.Length, culture ?? "default");

            QueryRequest request = new()
            {
                Columns = [.. columns],
                Measures = [.. measures],
                Filters = [.. filters],
                Sorts = [.. sorts],
                Culture = culture
            };

            var daxQuery = await queryService.GenerateDaxQueryAsync(request, cancellationToken);

            return JsonSerializer.Serialize(new { daxQuery }, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating DAX query");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }
}