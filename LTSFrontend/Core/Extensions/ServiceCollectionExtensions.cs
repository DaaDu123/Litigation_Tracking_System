using System.Net;
using LTSFrontend.Core.Auth;
using LTSFrontend.Core.Http;
using LTSFrontend.Features.Auth.Services;
using LTSFrontend.Features.Users.Services;
using LTSFrontend.State;
using Microsoft.AspNetCore.Components.Authorization;

namespace LTSFrontend.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLtsFrontendServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            // Blazor auth plumbing
            services.AddAuthorizationCore();
            services.AddCascadingAuthenticationState();

            // Session / token storage
            services.AddScoped<UserSessionState>();
            services.AddScoped<ITokenStorageService, TokenStorageService>();
            services.AddScoped<CustomAuthStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(
                sp => sp.GetRequiredService<CustomAuthStateProvider>());

            // HttpClient -> LTSBackend
            services.AddScoped<AuthTokenHandler>();
            services.AddScoped(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var env = sp.GetRequiredService<IHostEnvironment>();
                var baseUrl = config["ApiSettings:BaseUrl"] ?? "https://localhost:7167";

                var socketHandler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };

                if (env.IsDevelopment())
                {
                    // Local dev SSL cert validation override
                    socketHandler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                var authHandler = sp.GetRequiredService<AuthTokenHandler>();
                authHandler.InnerHandler = socketHandler;

                return new HttpClient(authHandler)
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(100)
                };
            });

            services.AddScoped<ApiClient>();

            // Feature services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            return services;
        }
    }
}