using FluentValidation;
using LTSBackend.Comman.Middleware;
using LTSBackend.Comman.Behaviors;
using LTSBackend.Data;
using LTSBackend.Features.Auth.Helpers;
using LTSBackend.Features.Authorization;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.BackgroundServices;
using LTSBackend.Services.CurrentUser;
using LTSBackend.Services.DocumentPermissions;
using LTSBackend.Services.Email;
using LTSBackend.Services.Jwt;
using LTSBackend.Services.LoginHistory;
using LTSBackend.Services.Permissions;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add application insights or other logging providers if needed
if (!builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}
#endregion

#region Database Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
#endregion

#region FluentValidation Configuration
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
#endregion

#region MediatR Configuration
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Pipeline behaviors for MediatR
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
#endregion

#region JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecret = jwtSettings["SecretKey"];

if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JwtSettings:SecretKey is missing. Check appsettings.json");

builder.Services.Configure<JwtSettings>(jwtSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),

            ClockSkew = TimeSpan.Zero
        };

        // ================================================================
        // Runs on EVERY authenticated request, after the token's signature
        // and expiry have already passed validation. This is what actually
        // enforces the SRS's "Active user status" step of the RBAC chain
        // and the security-stamp / session-revocation requirements: a JWT
        // can be perfectly valid and unexpired and STILL be rejected here
        // if the account was deactivated/blocked, the firm was
        // suspended/deleted, or the stamp no longer matches (password was
        // changed, or an admin forced logout) since the token was issued.
        // ================================================================
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Token is missing a valid user identifier.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                // IgnoreQueryFilters(): at this exact point HttpContext.User is
                // not yet the authenticated principal (that assignment happens
                // only once this event succeeds), so the tenant-scoping global
                // filter cannot resolve a FirmID yet - look the user up
                // directly and enforce every rule explicitly instead.
                var record = await dbContext.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(u => u.UserID == userId)
                    .Select(u => new
                    {
                        u.IsActive,
                        u.IsDeleted,
                        u.SecurityStamp,
                        FirmBlocked = u.Firm != null && u.Firm.IsBlocked,
                        FirmDeleted = u.Firm != null && u.Firm.IsDeleted
                    })
                    .FirstOrDefaultAsync();

                if (record == null || record.IsDeleted || !record.IsActive)
                {
                    context.Fail("This account is no longer active.");
                    return;
                }

                if (record.FirmBlocked || record.FirmDeleted)
                {
                    context.Fail("This firm's workspace has been suspended.");
                    return;
                }

                var tokenStamp = context.Principal?.FindFirstValue("SecurityStamp");
                if (string.IsNullOrEmpty(tokenStamp) || tokenStamp != record.SecurityStamp)
                {
                    context.Fail("Session has been invalidated. Please log in again.");
                }
            }
        };
    });
#endregion
builder.Services.AddHostedService<ReminderService>();
#region Service Registration
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Authentication & Security Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<CookieHelper>();

// Email & File Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();

// ✅ NEW - Document Permission Service (FOR MOHARRIR BLIND UPLOAD)
builder.Services.AddScoped<IDocumentPermissionService, DocumentPermissionService>();

// Audit & Logging Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// User & Permission Services
builder.Services.AddScoped<ILoginHistoryService, LoginHistoryService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Authorization Handler
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Dynamic permission-policy provider: PermissionPolicyProvider existed in
// the codebase but was never registered here, so it was dead code and any
// [HasPermission("X")] attribute for a permission NOT already hardcoded in
// AddAuthorization(...) below would throw "policy not found" at request
// time. Registering it lets [HasPermission("AnyPermissionName")] resolve
// dynamically for every permission, without needing a matching hardcoded
// policy for each one.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
#endregion

#region Background Services
builder.Services.AddHostedService<RefreshTokenCleanupService>();
#endregion

#region Controllers Configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
#endregion

#region CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    // Add stricter policy for production if needed
    if (!builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    }
});
#endregion

#region Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Define all permission-based policies
    var permissions = new[]
    {
        "ViewUsers",
        "CreateUsers",
        "UpdateUsers",
        "DeleteUsers",
        "ManageRoles",
        "ViewAuditLogs",
        "ViewDashboard",
        "ViewLoginHistory",
        "DeleteLoginHistory",
        "UploadDocuments",
        "ViewDocuments",
        "DownloadDocuments",
        "DeleteDocuments"
    };

    foreach (var permission in permissions)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});
#endregion

#region Rate Limiting Configuration
// SRS "API Security" explicitly calls for rate limiting; there was none in
// the pipeline previously, leaving login, OTP, and password-reset endpoints
// open to unlimited brute-force attempts regardless of the FailedLoginAttempts
// lockout counter on the User entity. Three policies are registered:
//  - Global limiter: a generous per-IP limit applied to every request.
//  - "auth-critical": a strict per-IP limit for endpoints that gate a
//    brute-forceable secret (login password, 6-digit OTP) - login,
//    verify-otp, refresh-token, reset-password.
//  - "auth-moderate": a slightly looser per-IP limit for endpoints that are
//    still abuse-prone (email-bombing/enumeration) but not directly
//    brute-forceable - register, resend-otp, forgot-password.
// AuthController applies these via [EnableRateLimiting("auth-critical")] /
// [EnableRateLimiting("auth-moderate")] on each endpoint.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // ================================================================
    // BUG FIX (CRITICAL): AuthController applies [EnableRateLimiting(...)]
    // with two policy names - "auth-critical" (login, verify-otp,
    // refresh-token, reset-password - endpoints that gate a brute-forceable
    // secret: password or a 6-digit OTP) and "auth-moderate" (register,
    // resend-otp, forgot-password - lower brute-force risk but still an
    // email-bombing/enumeration vector). Only a single policy named "auth"
    // was ever registered here, so every one of those seven endpoints would
    // throw "no IRateLimiterPolicy registered with the name ..." the moment
    // it was hit, taking down login/registration/password-reset entirely.
    // Both names the controller actually asks for are registered below;
    // "auth-critical" is intentionally the stricter of the two.
    // ================================================================
    options.AddPolicy("auth-critical", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("auth-moderate", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
#endregion

#region Health Check Configuration
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
#endregion

#region Forwarded Headers Configuration
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
#endregion

#region Swagger/OpenAPI Configuration - 🔴 CRITICAL FIX
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Litigation Tracking System API",
        Version = "v1.0.0",
        Description = "Complete Case Management System with Role-Based Access Control, Document Management, and Audit Logging",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "LTS Support Team",
            Email = "support@lts.local"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    // 🔴 CRITICAL: Enable annotations
    c.EnableAnnotations();

    // JWT Bearer Authentication Scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\nEnter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header
    });

    // Add JWT to all endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Include XML documentation comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Sort endpoints alphabetically
    c.OrderActionsBy(x => x.HttpMethod);
});
#endregion

var app = builder.Build();

#region Middleware Pipeline - 🔴 CRITICAL: ORDER MATTERS
// 1. Exception handling (must be first)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 1b. Request-level audit logging (incoming method/path/IP)
app.UseMiddleware<AuditMiddleware>();

// 2. Forwarded headers (for proxies)
app.UseForwardedHeaders();

// 3. Swagger (before HTTPS redirect for development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LTS API v1.0.0");
        c.RoutePrefix = ""; // Root path for Swagger UI
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        //c.DisplayOperationIds();
    });
}

// 4. HTTPS Redirect
app.UseHttpsRedirection();

// 5. Static files
app.UseStaticFiles();
app.UseRouting();   // Explicitly resolves the endpoint/routing decision here, before CORS/auth/authorization middleware run

// 6. CORS
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

// 7. Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

// 8. Authentication & Authorization
app.UseAuthentication();

// Rate limiting runs after authentication so partitioning could later be
// extended to per-user (not just per-IP) if needed, and before
// authorization so a rate-limited request never reaches a handler.
app.UseRateLimiter();

app.UseAuthorization();

// 9. Routing
app.MapControllers();
app.MapHealthChecks("/health");

#endregion

// Log startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("========================================");
logger.LogInformation("Litigation Tracking System API Starting");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Swagger UI: https://localhost:5001");
logger.LogInformation("========================================");

app.Run();