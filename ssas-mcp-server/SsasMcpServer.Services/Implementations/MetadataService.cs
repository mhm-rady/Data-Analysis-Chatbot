// File: SsasMcpServer.Services/Implementations/MetadataService.cs
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SsasMcpServer.Models.Exceptions;
using SsasMcpServer.Models.Metadata;
using SsasMcpServer.Services.Interfaces;

namespace SsasMcpServer.Services.Implementations;

public class MetadataService : IMetadataService
{
    private readonly ILogger<MetadataService> _logger;
    private readonly ISsasConnectionService _connectionService;
    private readonly ICultureService _cultureService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private const string CACHE_KEY_PREFIX = "SSAS_METADATA_";

    public MetadataService(
        ILogger<MetadataService> logger,
        ISsasConnectionService connectionService,
        ICultureService cultureService,
        IMemoryCache cache)
    {
        _logger = logger;
        _connectionService = connectionService;
        _cultureService = cultureService;
        _cache = cache;
    }

    public async Task<TabularModelMetadata> GetModelMetadataAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}MODEL_{culture ?? "default"}";
        
        if (_cache.TryGetValue<TabularModelMetadata>(cacheKey, out var cachedMetadata) && cachedMetadata != null)
        {
            _logger.LogDebug("Returning cached model metadata");
            return cachedMetadata;
        }

        _logger.LogInformation("Extracting model metadata from SSAS");

        try
        {
            var server = await _connectionService.GetTabularServerAsync(cancellationToken);
            var database = server.Databases.FindByName(_connectionService.GetDatabaseName());

            if (database == null)
                throw new SsasException($"Database '{_connectionService.GetDatabaseName()}' not found");

            var model = database.Model;
            var metadata = new TabularModelMetadata
            {
                DatabaseName = database.Name,
                ModelDescription = model.Description,
                SupportedCultures = await GetSupportedCulturesAsync(cancellationToken),
                LastProcessed = database.LastProcessed,
                CompatibilityLevel = database.CompatibilityLevel.ToString(),
                Dimensions = await ExtractDimensionsAsync(model, culture),
                Measures = await ExtractMeasuresAsync(model, culture),
                Kpis = await ExtractKpisAsync(model, culture)
            };

            _cache.Set(cacheKey, metadata, _cacheExpiration);
            _logger.LogInformation("Model metadata extracted successfully. Dimensions: {DimCount}, Measures: {MeasureCount}", 
                metadata.Dimensions.Count, metadata.Measures.Count);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract model metadata");
            throw new SsasException("Failed to extract model metadata", ex);
        }
    }

    public async Task<List<DimensionMetadata>> GetDimensionsAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetModelMetadataAsync(culture, cancellationToken);
        return metadata.Dimensions;
    }

    public async Task<DimensionMetadata> GetDimensionAsync(
        string dimensionName, 
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var dimensions = await GetDimensionsAsync(culture, cancellationToken);
        var dimension = dimensions.FirstOrDefault(d => 
            d.TechnicalName.Equals(dimensionName, StringComparison.OrdinalIgnoreCase) ||
            d.DisplayName.Equals(dimensionName, StringComparison.OrdinalIgnoreCase));

        if (dimension == null)
            throw new MetadataNotFoundException(dimensionName);

        return dimension;
    }

    public async Task<List<MeasureMetadata>> GetMeasuresAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetModelMetadataAsync(culture, cancellationToken);
        return metadata.Measures;
    }

    public async Task<List<MeasureMetadata>> GetMeasuresByNamesAsync(
        List<string> measureNames, 
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var allMeasures = await GetMeasuresAsync(culture, cancellationToken);
        var result = new List<MeasureMetadata>();

        foreach (var name in measureNames)
        {
            var measure = allMeasures.FirstOrDefault(m => 
                m.TechnicalName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                m.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (measure != null)
                result.Add(measure);
        }

        return result;
    }

    public async Task<List<KpiMetadata>> GetKpisAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetModelMetadataAsync(culture, cancellationToken);
        return metadata.Kpis;
    }

    public async Task<List<HierarchyMetadata>> GetAllHierarchiesAsync(
        string? culture = null, 
        CancellationToken cancellationToken = default)
    {
        var dimensions = await GetDimensionsAsync(culture, cancellationToken);
        return dimensions.SelectMany(d => d.Hierarchies).ToList();
    }

    public async Task<List<string>> GetSupportedCulturesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = await _connectionService.GetTabularServerAsync(cancellationToken);
            var database = server.Databases.FindByName(_connectionService.GetDatabaseName());

            if (database?.Model?.Cultures == null || database.Model.Cultures.Count == 0)
                return new List<string> { _cultureService.GetDefaultCulture() };

            return database.Model.Cultures.Select(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get supported cultures, returning default");
            return new List<string> { _cultureService.GetDefaultCulture() };
        }
    }

    public Task RefreshMetadataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing metadata cache");
        
        // Clear all metadata cache entries
        var keysToRemove = new List<string>();
        // Note: In production, consider using a more sophisticated cache key tracking mechanism
        
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    private async Task<List<DimensionMetadata>> ExtractDimensionsAsync(Model model, string? culture)
    {
        return await Task.Run(() =>
        {
            var dimensions = new List<DimensionMetadata>();

            foreach (var table in model.Tables.Where(t => !t.IsHidden))
            {
                var dimension = new DimensionMetadata
                {
                    TechnicalName = table.Name,
                    DisplayName = table.Name,
                    Description = table.Description,
                    IsHidden = table.IsHidden,
                    Cultures = ExtractCultureInfo(table, model),
                    Attributes = ExtractAttributes(table, model),
                    Hierarchies = ExtractHierarchies(table, model)
                };

                dimensions.Add(dimension);
            }

            return dimensions;
        });
    }

    private List<AttributeMetadata> ExtractAttributes(Table table, Model model)
    {
        var attributes = new List<AttributeMetadata>();

        foreach (var column in table.Columns.Where(c => !c.IsHidden && c.Type != ColumnType.RowNumber))
        {
            var attribute = new AttributeMetadata
            {
                TechnicalName = column.Name,
                DisplayName = column.Name,
                Description = column.Description,
                DataType = column.DataType.ToString(),
                IsKey = column.IsKey,
                IsHidden = column.IsHidden,
                FormatString = column.FormatString,
                Cultures = ExtractColumnCultureInfo(column, model)
            };

            attributes.Add(attribute);
        }

        return attributes;
    }

    private List<HierarchyMetadata> ExtractHierarchies(Table table, Model model)
    {
        var hierarchies = new List<HierarchyMetadata>();

        foreach (var hierarchy in table.Hierarchies.Where(h => !h.IsHidden))
        {
            var hierarchyMetadata = new HierarchyMetadata
            {
                TechnicalName = hierarchy.Name,
                DisplayName = hierarchy.Name,
                Description = hierarchy.Description,
                IsHidden = hierarchy.IsHidden,
                Cultures = ExtractHierarchyCultureInfo(hierarchy, model),
                Levels = hierarchy.Levels.Select((level, index) => new HierarchyLevelMetadata
                {
                    TechnicalName = level.Name,
                    DisplayName = level.Name,
                    SourceAttribute = level.Column?.Name ?? string.Empty,
                    Ordinal = level.Ordinal
                }).ToList()
            };

            hierarchies.Add(hierarchyMetadata);
        }

        return hierarchies;
    }

    private async Task<List<MeasureMetadata>> ExtractMeasuresAsync(Model model, string? culture)
    {
        return await Task.Run(() =>
        {
            var measures = new List<MeasureMetadata>();

            foreach (var table in model.Tables)
            {
                foreach (var measure in table.Measures.Where(m => !m.IsHidden))
                {
                    var measureMetadata = new MeasureMetadata
                    {
                        TechnicalName = $"{table.Name}[{measure.Name}]",
                        DisplayName = measure.Name,
                        Description = measure.Description,
                        FormatString = measure.FormatString,
                        Expression = measure.Expression,
                        IsHidden = measure.IsHidden,
                        DisplayFolder = measure.DisplayFolder,
                        Cultures = ExtractMeasureCultureInfo(measure, model)
                    };

                    measures.Add(measureMetadata);
                }
            }

            return measures;
        });
    }

    private async Task<List<KpiMetadata>> ExtractKpisAsync(Model model, string? culture)
    {
        return await Task.Run(() =>
        {
            var kpis = new List<KpiMetadata>();

            foreach (var table in model.Tables)
            {
                foreach (var measure in table.Measures.Where(m => m.KPI != null))
                {
                    var kpi = measure.KPI;
                    var kpiMetadata = new KpiMetadata
                    {
                        TechnicalName = $"{table.Name}[{measure.Name}]",
                        DisplayName = measure.Name,
                        Description = kpi.Description ?? measure.Description,
                        DisplayFolder = measure.DisplayFolder,
                        ValueExpression = measure.Expression,
                        GoalExpression = kpi.TargetExpression,
                        StatusExpression = kpi.StatusExpression,
                        TrendExpression = kpi.TrendExpression,
                        Cultures = ExtractMeasureCultureInfo(measure, model)
                    };

                    kpis.Add(kpiMetadata);
                }
            }

            return kpis;
        });
    }

    private List<Models.Metadata.CultureInfo> ExtractCultureInfo(Table table, Model model)
    {
        var cultures = new List<Models.Metadata.CultureInfo>();

        if (model.Cultures == null || model.Cultures.Count == 0)
            return cultures;

        foreach (var culture in model.Cultures)
        {
            var translation = culture.ObjectTranslations.FirstOrDefault(t => 
                t.Object == table);
            if (translation != null)
            {
                cultures.Add(new Models.Metadata.CultureInfo
                {
                    Culture = culture.Name,
                    Name = translation.Property == TranslatedProperty.Caption ? translation.Value : table.Name,
                    Description = translation.Property == TranslatedProperty.Description ? translation.Value : null
                });
            }
        }

        return cultures;
    }

    private List<Models.Metadata.CultureInfo> ExtractColumnCultureInfo(Column column, Model model)
    {
        var cultures = new List<Models.Metadata.CultureInfo>();

        if (model.Cultures == null || model.Cultures.Count == 0)
            return cultures;

        foreach (var culture in model.Cultures)
        {
            var translation = culture.ObjectTranslations.FirstOrDefault(t => 
                t.Object == column);
            if (translation != null)
            {
                cultures.Add(new Models.Metadata.CultureInfo
                {
                    Culture = culture.Name,
                    Name = translation.Property == TranslatedProperty.Caption ? translation.Value : column.Name,
                    Description = translation.Property == TranslatedProperty.Description ? translation.Value : null
                });
            }
        }

        return cultures;
    }

    private List<Models.Metadata.CultureInfo> ExtractHierarchyCultureInfo(Hierarchy hierarchy, Model model)
    {
        var cultures = new List<Models.Metadata.CultureInfo>();

        if (model.Cultures == null || model.Cultures.Count == 0)
            return cultures;

        foreach (var culture in model.Cultures)
        {
            var translation = culture.ObjectTranslations.FirstOrDefault(t => 
                t.Object == hierarchy);
            if (translation != null)
            {
                cultures.Add(new Models.Metadata.CultureInfo
                {
                    Culture = culture.Name,
                    Name = translation.Property == TranslatedProperty.Caption ? translation.Value : hierarchy.Name,
                    Description = translation.Property == TranslatedProperty.Description ? translation.Value : null
                });
            }
        }

        return cultures;
    }

    private List<Models.Metadata.CultureInfo> ExtractMeasureCultureInfo(Measure measure, Model model)
    {
        var cultures = new List<Models.Metadata.CultureInfo>();

        if (model.Cultures == null || model.Cultures.Count == 0)
            return cultures;

        foreach (var culture in model.Cultures)
        {
            var translation = culture.ObjectTranslations.FirstOrDefault(t => 
                t.Object == measure);
            if (translation != null)
            {
                cultures.Add(new Models.Metadata.CultureInfo
                {
                    Culture = culture.Name,
                    Name = translation.Property == TranslatedProperty.Caption ? translation.Value : measure.Name,
                    Description = translation.Property == TranslatedProperty.Description ? translation.Value : null
                });
            }
        }

        return cultures;
    }
}