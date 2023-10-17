using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BIAB.WebAPI.Enums;
using BIAB.WebAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace BIAB.WebAPI;

public static class SetupExtensions
{
    private static TokenValidationParameters _defaultTokenValidationParameters(ApiSettings settings)
    {
        return new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            RequireAudience = true,
            ValidAudience = settings.JwtAudience,
            ValidIssuer = settings.JwtIssuer,
        };
    }

    /// <summary>
    /// Adds a basic JWT authentication scheme to the application.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="settings"></param>
    /// <param name="period">Rolling Period</param>
    /// <param name="authenticationOptions"></param>
    /// <param name="jwtBearerOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, ApiSettings settings, RollingPeriod period = RollingPeriod.None, 
        Action<AuthenticationOptions>? authenticationOptions = null, Action<JwtBearerOptions>? jwtBearerOptions = null)
    {
        authenticationOptions ??= options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        };
        
        if (jwtBearerOptions == null)
        {
            TokenValidationParameters tokenValidationParameters = _defaultTokenValidationParameters(settings);
            if (period == RollingPeriod.None)
            {
                tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret));
            }
            else
            {
                tokenValidationParameters.IssuerSigningKeyResolver = (_, _, _, _) =>
                {
                    // use the jwt secret as a base for the rolling key
                    // then use the current date to generate a new key

                    // get the current date
                    var now = DateTime.UtcNow;

                    var secretString = period switch
                    {
                        RollingPeriod.None => settings.JwtSecret,
                        RollingPeriod.Daily => $"{settings.JwtSecret}{now:yyyy-MM-dd}",
                        RollingPeriod.Monthly => $"{settings.JwtSecret}{now:yyyy-MM}",
                        RollingPeriod.Weekly => $"{settings.JwtSecret}{now:yyyy}-{now:MM}-{now:dd}",
                        RollingPeriod.Yearly => $"{settings.JwtSecret}{now:yyyy}",
                        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
                    };

                    SymmetricSecurityKey secret = new(Encoding.UTF8.GetBytes(secretString));

                    return new[] { secret };
                };
            }
        
            jwtBearerOptions = options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = tokenValidationParameters;
            };
        }
        
        services.AddAuthentication(authenticationOptions).AddJwtBearer(jwtBearerOptions);
        
        return services;
    }
    
    /// <summary>
    /// Adds Identity With Password Requirements to the Application.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TRole"></typeparam>
    /// <typeparam name="TDb"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddBasicIdentity<TUser, TRole, TDb>(this IServiceCollection services, Action<IdentityOptions>? options = null)
        where TUser : IdentityUser where TRole : IdentityRole where TDb : IdentityDbContext<TUser>
    {
        options ??= o =>
        {
            o.Password.RequireDigit = true;
            o.Password.RequireLowercase = true;
            o.Password.RequireUppercase = true;
            o.Password.RequireNonAlphanumeric = true;
            o.Password.RequiredLength = 7;
        };
        services.AddIdentity<TUser, TRole>(options)
            .AddEntityFrameworkStores<TDb>()
            .AddDefaultTokenProviders();
        return services;
    }
    
    /// <summary>
    /// Adds Authentication and Authorization to the Application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseJwtAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
    
    /// <summary>
    /// Runs Migrations on the Database If it is not up to date.
    /// </summary>
    /// <param name="app"></param>
    /// <typeparam name="TDb"></typeparam>
    /// <returns></returns>
    public static WebApplication AutoMigrateDb<TDb>(this WebApplication app) where TDb : DbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDb>();
        db.Database.Migrate();
        return app;
    }
    
    /// <summary>
    /// Gets Settings and Adds them to the Service Collection.
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TSettings"></typeparam>
    /// <returns></returns>
    public static TSettings AddSettings<TSettings>(this WebApplicationBuilder builder) where TSettings : ApiSettings, new()
    {
        TSettings settings = new TSettings();
        settings.SetConfiguration(builder.Configuration);
        builder.Services.AddSingleton<ApiSettings>(settings);
        builder.Services.AddSingleton(settings);
        return settings;
    }
    
    /// <summary>
    /// Adds Swagger and Disables Cors When in Development Environment
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication DevelopmentCorsAndSwaggerOverride(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors(builder => builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin());
        }

        return app;
    }
    
    /// <summary>
    /// Adds Basic Whoami Endpoints for Testing Authentication
    /// </summary>
    /// <param name="app"></param>
    public static void MapJwtWhoami(this WebApplication app)
    {
        // Basic Api Endpoints for JWT Authentication
        // Using Minimal api
        
        app.MapGet("/auth/whoami", (ClaimsPrincipal user) =>
        {
            return Results.Ok(user.Identity!.Name);
        }).RequireAuthorization();
        
        app.MapGet("/auth/whoami/claims", (ClaimsPrincipal user) =>
        {
            return Results.Ok(user.Claims.Select(c => new { c.Type, c.Value }));
        }).RequireAuthorization();
        
        app.MapGet("/auth/whoami/roles", (ClaimsPrincipal user) =>
        {
            return Results.Ok(user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
        }).RequireAuthorization();

    }


    /// <summary>
    /// Takes a Register Model and Returns a User
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TUser"></typeparam>
    public delegate Task<TUser> GetUserDelegate<TModel, TUser>(TModel model) where TUser : IdentityUser where TModel : RegisterModel;
    
    /// <summary>
    /// Delegate to get the Expiration Time for a Token
    /// </summary>
    public delegate DateTime GetDateTimeDelegate();
    
    
    /// <summary>
    /// Adds Login and Register Endpoints for JWT Authentication
    /// </summary>
    /// <param name="app"></param>
    /// <param name="settings"></param>
    /// <param name="getUserDelegate"></param>
    /// <param name="getExpireTime"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TUser"></typeparam>
    public static void MapJwtEndpoints<TModel, TUser>(this WebApplication app, ApiSettings settings, GetUserDelegate<TModel, TUser> getUserDelegate, GetDateTimeDelegate? getExpireTime = null) where TUser : IdentityUser where TModel : RegisterModel
    {
        // Basic Api Endpoints for JWT Authentication
        // Using Minimal api
        
        getExpireTime ??= () => DateTime.UtcNow.AddDays(7);
        
        // Register a new user
        app.MapPost("/auth/register", async (TModel model, UserManager<TUser> userManager) =>
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return Results.BadRequest();
            
            // Create User
            var user = await getUserDelegate(model);
            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return Results.Ok();
            }
            else
            {
                return Results.BadRequest(result.Errors);
            }
        });
        
        // Login a user
        app.MapPost("/auth/login", async (LoginModel model, UserManager<TUser> userManager) =>
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return Results.BadRequest();
            
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!),
                };
                var roles = await userManager.GetRolesAsync(user);
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
                var claimsIdentity = new ClaimsIdentity(claims, "jwt");
                
                // Create JWT Token with valid issuer and audience
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(settings.JwtSecret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = claimsIdentity,
                    Expires = getExpireTime(),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = settings.JwtIssuer,
                    Audience = settings.JwtAudience
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
            }
            else
            {
                return Results.BadRequest("Username or password incorrect.");
            }
        });
    }

    /// <summary>
    /// Adds a Delete Endpoint for Accounts
    /// </summary>
    /// <param name="app"></param>
    public static void MapJwtDeleteEndpoint(this WebApplication app)
    {
        // Delete a user
        app.MapDelete("/auth/delete", async (ClaimsPrincipal user, UserManager<IdentityUser> userManager) =>
        {
            var identityUser = await userManager.FindByEmailAsync(user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? throw new UnauthorizedAccessException());
            if (identityUser != null)
            {
                var result = await userManager.DeleteAsync(identityUser);
                if (result.Succeeded)
                {
                    return Results.Ok();
                }
                else
                {
                    return Results.BadRequest(result.Errors);
                }
            }
            else
            {
                return Results.BadRequest("User not found.");
            }
        }).RequireAuthorization();
    }
}