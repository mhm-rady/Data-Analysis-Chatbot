using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SsasMcpServer.Models.Configuration;
using SsasMcpServer.Services.Interfaces;

namespace SsasMcpServer.Services.Implementations;

public class CultureService : ICultureService
{
    private readonly ILogger<CultureService> _logger;
    private readonly McpServerOptions _options;

    public CultureService(
        ILogger<CultureService> logger,
        IOptions<McpServerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public string GetLocalizedName(
        string technicalName, 
        List<Models.Metadata.CultureInfo> cultures, 
        string? culture)
    {
        if (string.IsNullOrEmpty(culture) || cultures.Count == 0)
            return technicalName;

        var normalizedCulture = NormalizeCultureCode(culture);
        var cultureInfo = cultures.FirstOrDefault(c => 
            c.Culture.Equals(normalizedCulture, StringComparison.OrdinalIgnoreCase));

        return cultureInfo?.Name ?? technicalName;
    }

    public string? GetLocalizedDescription(
        List<Models.Metadata.CultureInfo> cultures, 
        string? culture)
    {
        if (string.IsNullOrEmpty(culture) || cultures.Count == 0)
            return cultures.FirstOrDefault()?.Description;

        var normalizedCulture = NormalizeCultureCode(culture);
        var cultureInfo = cultures.FirstOrDefault(c => 
            c.Culture.Equals(normalizedCulture, StringComparison.OrdinalIgnoreCase));

        return cultureInfo?.Description;
    }

    public bool IsCultureSupported(string culture)
    {
        if (string.IsNullOrEmpty(culture))
            return true;

        var normalizedCulture = NormalizeCultureCode(culture);
        return _options.SupportedCultures.Any(c => 
            c.Equals(normalizedCulture, StringComparison.OrdinalIgnoreCase));
    }

    public string GetDefaultCulture() => _options.DefaultCulture;

    public string NormalizeCultureCode(string culture)
    {
        if (string.IsNullOrEmpty(culture))
            return _options.DefaultCulture;

        // Handle short codes like "en" -> "en-US", "ar" -> "ar-SA"
        if (!culture.Contains('-'))
        {
            var matchingCulture = _options.SupportedCultures.FirstOrDefault(c => 
                c.StartsWith(culture + "-", StringComparison.OrdinalIgnoreCase));
            
            if (matchingCulture != null)
                return matchingCulture;
        }

        return culture;
    }
}