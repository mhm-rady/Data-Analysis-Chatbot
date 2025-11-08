
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SsasMcpServer.Services.Interfaces;
using System.ComponentModel;
using System.Text.Json;

namespace SsasMcpServer.Tools;

[McpServerToolType]
public sealed class MetadataTools
{
    [McpServerTool, Description("Retrieve complete SSAS tabular model metadata including dimensions, measures, KPIs, hierarchies, and supported cultures. Use this to understand the available analytical objects in the model.")]
    public static async Task<string> GetModelMetadata(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        [Description("Optional culture code (e.g., 'en-US', 'ar-SA') for localized names and descriptions. If not provided, default culture is used.")] string? culture,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting model metadata for culture: {Culture}", culture ?? "default");

            var metadata = await metadataService.GetModelMetadataAsync(culture, cancellationToken);

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting model metadata");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Retrieve all dimensions (tables) with their attributes (columns) and hierarchies. Essential for understanding available data structure for building queries.")]
    public static async Task<string> GetDimensions(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        [Description("Optional culture code (e.g., 'en-US', 'ar-SA') for localized names and descriptions. If not provided, default culture is used.")] string? culture,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting dimensions for culture: {Culture}", culture ?? "default");

            var dimensions = await metadataService.GetDimensionsAsync(culture, cancellationToken);

            return JsonSerializer.Serialize(dimensions, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dimensions");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Retrieve a specific dimension (table) by name along with its attributes (columns) and hierarchies. Useful for detailed inspection of a particular data structure.")]
    public static async Task<string> GetDimension(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        [Description("Name of the dimension (table) to retrieve.")] string dimensionName,
        [Description("Optional culture code (e.g., 'en-US', 'ar-SA') for localized names and descriptions. If not provided, default culture is used.")] string? culture,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting dimension '{Dimension}' for culture: {Culture}", dimensionName, culture ?? "default");

            var dimension = await metadataService.GetDimensionAsync(dimensionName, culture, cancellationToken);

            return JsonSerializer.Serialize(dimension, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dimension '{Dimension}'", dimensionName);
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Retrieve all measures (calculations) with their details. Essential for understanding available calculations in the model.")]
    public static async Task<string> GetMeasures(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        [Description("Optional culture code (e.g., 'en-US', 'ar-SA') for localized names and descriptions. If not provided, default culture is used.")] string? culture,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting measures for culture: {Culture}", culture ?? "default");

            var measures = await metadataService.GetMeasuresAsync(culture, cancellationToken);

            return JsonSerializer.Serialize(measures, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting measures");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Retrieve all KPIs (Key Performance Indicators) with their details. Useful for understanding performance metrics defined in the model.")]
    public static async Task<string> GetKPIs(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        string? culture,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting KPIs for culture: {Culture}", culture ?? "default");

            var kpis = await metadataService.GetKpisAsync(culture, cancellationToken);

            return JsonSerializer.Serialize(kpis, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting KPIs");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Retrieve all hierarchies with their levels and attributes. Important for understanding data organization and navigation paths.")]
    public static async Task<string> GetHierarchies(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        string? culture,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting hierarchies for culture: {Culture}", culture ?? "default");

            var hierarchies = await metadataService.GetAllHierarchiesAsync(culture, cancellationToken);

            return JsonSerializer.Serialize(hierarchies, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting hierarchies");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }

    [McpServerTool, Description("Get list of all supported culture codes in the SSAS model. Use this to determine available localizations.")]
    public static async Task<string> GetSupportedCultures(
        IMetadataService metadataService,
        ILogger<MetadataTools> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Getting supported cultures");

            var cultures = await metadataService.GetSupportedCulturesAsync(cancellationToken);

            return JsonSerializer.Serialize(cultures, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting supported cultures");
            return JsonSerializer.Serialize(new { error = ex.Message, type = ex.GetType().Name });
        }
    }
    
    

}