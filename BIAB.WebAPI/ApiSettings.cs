using Microsoft.Extensions.Configuration;

namespace BIAB.WebAPI;

/// <summary>
/// A Simple Configuration Mapper
/// </summary>
public class ApiSettings
{
    protected IConfiguration? Configuration { get; set; }
    public ApiSettings(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public ApiSettings()
    {
        Configuration = null;
    }
    
    /// <summary>
    /// Sets the Configuration Used for the Various Settings
    /// </summary>
    /// <param name="configuration"></param>
    public void SetConfiguration(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    // Public Url for this API
    public string ApiUrl => Configuration?["ApiUrl"] ?? "";
    
    // JWT Secret Key for Signing Tokens
    public string JwtSecret => Configuration?["JwtSecret"] ?? "big secret that should not be here because that is not a secure way to do this";
    
    // JWT Issuer for Signing Tokens
    public string JwtIssuer => Configuration?["JwtIssuer"] ?? "localhost:7269";
    
    // JWT Audience for Signing Tokens
    public string JwtAudience => Configuration?["JwtAudience"] ?? "localhost:7214";
    
    // Database Connection String
    public string DbConnectionString => Configuration?.GetConnectionString("BudgetContext") ?? "Data Source=Budgeting.db";
}