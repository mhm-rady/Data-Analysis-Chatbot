using System.ComponentModel.DataAnnotations;

namespace SsasMcpServer.Models.Configuration;

public class SsasConnectionOptions
{
    public const string SectionName = "SsasConnection";

    [Required]
    public string Server { get; set; } = string.Empty;

    [Required]
    public string Database { get; set; } = string.Empty;

    public int Timeout { get; set; } = 300;

    public bool UseWindowsAuth { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string BuildConnectionString()
    {
        var connString = $"Data Source={Server};Catalog={Database};";
        
        if (UseWindowsAuth)
        {
            connString += "Integrated Security=SSPI;";
        }
        else if (!string.IsNullOrEmpty(Username))
        {
            connString += $"User ID={Username};Password={Password};";
        }
        
        if (Timeout > 0)
        {
            connString += $"Connect Timeout={Timeout};";
        }
        
        return connString;
    }
}