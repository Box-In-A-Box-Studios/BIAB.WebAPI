# Box in a Box Web API Library
Adds a extensions for .Net 6 and .Net 7 Web APIs to more easily configure Authentication, Authorization, and CORS.

## Getting Started
### Installation
#### Option 1: From Source
1. Get a copy of a Release DLL.
2. Open your project in Visual Studio.
3. Right click on your project and select "Add Reference".
4. Select "Browse" and navigate to the DLL.
5. Select the DLL and click "Add".
6. Click "OK" to close the "Add Reference" dialog.

#### Option 2: From NuGet
1. Open your project in Visual Studio.
2. Right click on your project and select "Manage NuGet Packages".
3. Select "Browse" and search for "BIAB.WebAPI".
4. Select the package and click "Install".

### Usage
Here is an Example Program.cs file that uses the library.
Please note that the DbContext needs to be replaced with one from your project.
```csharp
using BIAB.WebAPI;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Get the Settings
ApiSettings settings = builder.AddSettings<ApiSettings>();

// Basic WebApi Steps
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger Options for Swagger Generation
builder.Services.AddSwaggerGen(c =>
{
    c.AddSwaggerJwtBearer(); // Adds the JWT Bearer authentication scheme to the Swagger UI.
});

// Add the DbContext
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlite(settings.DbConnectionString));

// For Identity
builder.Services.AddBasicIdentity<IdentityUser, IdentityRole, IdentityDbContext>();

// Adding Authentication
builder.Services.AddJwtAuthentication(settings);

// Build the WebApp
var app = builder.Build();

// Development Only - Enables Swagger UI and Disable CORS
app.DevelopmentCorsAndSwaggerOverride();

// Adds Https Redirection
app.UseHttpsRedirection();

// Adds Authentication
app.UseJwtAuthentication();

// Basic WebApi Steps
app.MapControllers();

// Basic Api Endpoints for JWT Authentication (Register, Login, Refresh, and Revoke)
app.MapJwtEndpoints(settings, async (RegisterModel model) => new IdentityUser
{
    UserName = model.Email,
    Email = model.Email
});
app.MapJwtDeleteEndpoint(); // Adds the ability to delete an account.

// Run Migrations Automatically for EF Core DbContexts
app.AutoMigrateDb<IdentityDbContext>();

// Run the App
app.Run();
```

### Example Settings
```csharp
using BIAB.WebAPI;

public class CustomSettings : ApiSettings
{
    public string ImportantSetting => Configuration?["ImportantSetting"] ?? "Default Value";
}
```
