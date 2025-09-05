using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.IdentityModel.Tokens;
using Synaptic.NET.Authentication.Controllers;
using Synaptic.NET.Authentication.Handlers;
using Synaptic.NET.Authentication.Middlewares;
using Synaptic.NET.Authentication.Providers;
using Synaptic.NET.Authentication.Services;
using Synaptic.NET.Core;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Configuration;

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

        app.Services.AddSingleton<ISymLinkUserService, SymLinkUserService>();
        app.Services.AddSingleton<RedirectUriProvider>();
        app.Services.AddSingleton<CodeBasedAuthProvider>();
        app.Services.AddSingleton<ISecurityTokenHandler, JwtTokenHandler>();
        app.Services.AddSingleton<IRefreshTokenHandler, RefreshTokenHandler>();

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
            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });
        app.Services.AddAuthorization();

        app.Services.AddControllersWithViews().ConfigureApplicationPartManager(manager =>
        {
            var authAssembly = typeof(AccountController).Assembly;
            manager.ApplicationParts.Clear();
            manager.ApplicationParts.Add(new AssemblyPart(authAssembly));

            manager.FeatureProviders.Add(new AuthControllerFeatureProvider());
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
        return app;
    }
}
