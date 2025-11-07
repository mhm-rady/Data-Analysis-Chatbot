using SsasMcpServer.Models.Metadata;

namespace SsasMcpServer.Services.Interfaces;

/// <summary>
/// Service for extracting and managing SSAS tabular model metadata
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Gets complete tabular model metadata including all cultures
    /// </summary>
    Task<TabularModelMetadata> GetModelMetadataAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets metadata for all dimensions with culture support
    /// </summary>
    Task<List<DimensionMetadata>> GetDimensionsAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets metadata for a specific dimension
    /// </summary>
    Task<DimensionMetadata> GetDimensionAsync(
        string dimensionName, 
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets metadata for all measures
    /// </summary>
    Task<List<MeasureMetadata>> GetMeasuresAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets metadata for specific measures
    /// </summary>
    Task<List<MeasureMetadata>> GetMeasuresByNamesAsync(
        List<string> measureNames, 
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets metadata for all KPIs
    /// </summary>
    Task<List<KpiMetadata>> GetKpisAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all hierarchies across all dimensions
    /// </summary>
    Task<List<HierarchyMetadata>> GetAllHierarchiesAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets supported cultures in the model
    /// </summary>
    Task<List<string>> GetSupportedCulturesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refreshes the metadata cache
    /// </summary>
    Task RefreshMetadataAsync(CancellationToken cancellationToken = default);
}
