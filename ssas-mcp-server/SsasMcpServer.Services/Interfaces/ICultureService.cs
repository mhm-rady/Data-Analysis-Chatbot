namespace SsasMcpServer.Services.Interfaces;

/// <summary>
/// Service for handling multi-language culture support
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Gets the display name for an object in specified culture
    /// </summary>
    string GetLocalizedName(string technicalName, List<Models.Metadata.CultureInfo> cultures, string? culture);
    
    /// <summary>
    /// Gets the description for an object in specified culture
    /// </summary>
    string? GetLocalizedDescription(List<Models.Metadata.CultureInfo> cultures, string? culture);
    
    /// <summary>
    /// Validates if a culture is supported
    /// </summary>
    bool IsCultureSupported(string culture);
    
    /// <summary>
    /// Gets the default culture
    /// </summary>
    string GetDefaultCulture();
    
    /// <summary>
    /// Normalizes culture code (e.g., "en" to "en-US")
    /// </summary>
    string NormalizeCultureCode(string culture);
}