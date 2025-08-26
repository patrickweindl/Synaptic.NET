using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.IdentityModel.Tokens;
using Synaptic.NET.Authentication.Components;
using Synaptic.NET.Authentication.Controllers;
using Synaptic.NET.Authentication.Middlewares;
using Synaptic.NET.Authentication.Providers;
using Synaptic.NET.Authentication.Services;
using Synaptic.NET.Core;
using Synaptic.NET.Domain;

namespace Synaptic.NET.Authentication;

public static class AuthenticationServices
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.ConfigureCoreServices();
        builder.ConfigureDomainServices(out var synapticSettings);
        builder.ConfigureAuthenticationAndAuthorization(synapticSettings);

        var app = builder.Build();

        app.ConfigureCoreApplication(synapticSettings);
        app.ConfigureAuthenticationAndAuthorizationAndMiddlewares();
        app.Run();
    }

    public static IHostApplicationBuilder ConfigureAuthenticationAndAuthorization(this WebApplicationBuilder app, SynapticServerSettings configuration)
    {
        app.Services.AddHttpContextAccessor();
        app.Services.AddAuthenticationCore();
        app.Services.AddCascadingAuthenticationState();
        app.Services.AddDataProtection();

        app.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
        app.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        app.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    LogValidationExceptions = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.JwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration.JwtIssuer,
                    ValidateAudience = false
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Log.Warning("[JWT Authorization] Unauthenticated request: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Log.Information("[JWT Authorization] User {User} authenticated successfully", context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Log.Warning("[JWT Authorization] Authentication challenge: {Message}", context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });
        app.Services.AddAuthorization();

        app.Services.AddControllers().ConfigureApplicationPartManager(manager =>
        {
            var authAssembly = typeof(AccountController).Assembly;
            manager.ApplicationParts.Clear();
            manager.ApplicationParts.Add(new AssemblyPart(authAssembly));

            manager.FeatureProviders.Add(new AuthControllerFeatureProvider());
        });

        app.Services.AddRazorPages().ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Clear();
            manager.ApplicationParts.Add(new AssemblyPart(typeof(AuthenticationApp).Assembly));
        });

        return app;
    }

    public static WebApplication ConfigureAuthenticationAndAuthorizationAndMiddlewares(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var mw = new LogAuthorizedRequestsMiddleware();
            await mw.Function.Invoke(context, next);
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapControllers();
        app.MapRazorPages();
        return app;
    }
}
