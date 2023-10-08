using System.Text;
using BIAB.WebAPI.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
}