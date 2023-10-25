using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BIAB.WebAPI.Enums;
using BIAB.WebAPI.Shared.Models;
using BIAB.WebAPI.Shared.Responses;
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
                tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret));
            else
                tokenValidationParameters.IssuerSigningKeyResolver = RollingIssuerSigningKeyResolver(settings, period);
            
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

    private static IssuerSigningKeyResolver RollingIssuerSigningKeyResolver(ApiSettings settings, RollingPeriod period)
    {
        return (_, _, _, _) =>
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
    
    private static async Task<IResult> RunAuthenticated<TUser>(UserManager<TUser> manager, ClaimsPrincipal user,
        Func<TUser, Task<IResult>> authenticatedAction) where TUser : IdentityUser
    {
        var identityUser = await manager.FindByEmailAsync(user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? throw new UnauthorizedAccessException());
        return identityUser != null ? await authenticatedAction(identityUser) : Results.BadRequest("User not found.");
    }

    
    /// <summary>
    /// Adds Login, Refresh, and Revoke Endpoints for JWT Authentication
    /// </summary>
    /// <param name="app"></param>
    /// <param name="settings"></param>
    /// <param name="getExpireTime"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TUser"></typeparam>
    public static void MapJwtEndpoints<TUser>(this WebApplication app, ApiSettings settings, GetDateTimeDelegate? getExpireTime = null) where TUser : IdentityUser
    {
        getExpireTime ??= () => DateTime.UtcNow.AddDays(7);
        
        // Login a user
        app.MapPost("/auth/login", async (LoginModel model, UserManager<TUser> userManager) =>
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return Results.BadRequest();
            
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
                return await GetTokenResult(settings, getExpireTime, user, userManager);
            return Results.BadRequest("Username or password incorrect.");
        });
        
        // Refresh a token
        app.MapPost("/auth/refresh", async (ClaimsPrincipal user, UserManager<TUser> userManager) => 
            await RunAuthenticated(userManager, user, async identityUser => 
                await GetTokenResult(settings, getExpireTime, identityUser, userManager))
            ).RequireAuthorization();
        
        // Revoke a token
        app.MapPost("/auth/revoke", async (ClaimsPrincipal user, UserManager<TUser> userManager) => 
            await RunAuthenticated(userManager, user, async identityUser =>
            {
                await userManager.UpdateSecurityStampAsync(identityUser);
                return Results.Ok();
            })
            ).RequireAuthorization();
    }
    
    /// <summary>
    /// Adds Register Endpoint for JWT Authentication
    /// </summary>
    /// <param name="app"></param>
    /// <param name="getUserDelegate"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TUser"></typeparam>
    public static void MapJwtRegisterEndpoint<TModel, TUser>(this WebApplication app, GetUserDelegate<TModel, TUser> getUserDelegate) where TUser : IdentityUser where TModel : RegisterModel
    {
        // Register a new user
        app.MapPost("/auth/register", async (TModel model, UserManager<TUser> userManager) =>
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                return Results.BadRequest();
            
            // Create User
            var user = await getUserDelegate(model);
            var result = await userManager.CreateAsync(user, model.Password);
            return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
        });
    }

    /// <summary>
    /// Creates a Token Result for a User
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="getExpireTime"></param>
    /// <param name="identityUser"></param>
    /// <param name="userManager"></param>
    /// <typeparam name="TUser"></typeparam>
    /// <returns></returns>
    private static async Task<IResult> GetTokenResult<TUser>(ApiSettings settings, GetDateTimeDelegate getExpireTime,
        TUser identityUser, UserManager<TUser> userManager) where TUser : IdentityUser
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, identityUser.UserName!),
            new Claim(ClaimTypes.Email, identityUser.Email!),
        };
        var roles = await userManager.GetRolesAsync(identityUser);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var claimsIdentity = new ClaimsIdentity(claims, "jwt");

        // Create JWT Token with valid issuer and audience
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(settings.JwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = getExpireTime(),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = settings.JwtIssuer,
            Audience = settings.JwtAudience
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        LoginResponse response = new LoginResponse
        {
            Token = tokenHandler.WriteToken(token),
            Expiration = token.ValidTo
        };
        return Results.Ok(response);
    }

    /// <summary>
    /// Adds a Delete Endpoint for Accounts
    /// </summary>
    /// <param name="app"></param>
    public static void MapJwtDeleteEndpoint<TUser>(this WebApplication app) where TUser : IdentityUser
    {
        // Delete a user
        app.MapDelete("/auth/delete", async (ClaimsPrincipal user, UserManager<TUser> userManager) =>
        await RunAuthenticated(userManager, user, async identityUser =>
        {
            var result = await userManager.DeleteAsync(identityUser);
            return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
        })).RequireAuthorization();
    }
}