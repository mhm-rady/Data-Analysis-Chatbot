namespace SsasMcpServer.Models.Configuration;

public class McpServerOptions
{
    public const string SectionName = "McpServer";

    public string Name { get; set; } = "ssas-mcp-server";
    public string Version { get; set; } = "1.0.0";
    public int MaxResultRows { get; set; } = 10000;
    public string DefaultCulture { get; set; } = "en-US";
    public List<string> SupportedCultures { get; set; } = new() { "en-US" };
}