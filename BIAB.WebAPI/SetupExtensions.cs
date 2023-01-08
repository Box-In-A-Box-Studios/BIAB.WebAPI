using System.Text;
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
    /// <summary>
    /// Adds a basic JWT authentication scheme to the application.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, ApiSettings settings)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = settings.JwtAudience,
                    ValidIssuer = settings.JwtIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret))
                };
            });
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
    public static IServiceCollection AddBasicIdentity<TUser, TRole, TDb>(this IServiceCollection services)
        where TUser : IdentityUser where TRole : IdentityRole where TDb : IdentityDbContext<TUser>
    {
        services.AddIdentity<TUser, TRole>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = true;
                o.Password.RequiredLength = 7;
            })
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