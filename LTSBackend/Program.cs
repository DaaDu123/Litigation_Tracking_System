using FluentValidation;
using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Authorization;
using LTSBackend.Features.Users.Commands.CreateUser;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using LTSBackend.Services.BackgroundServices;
using LTSBackend.Services.Email;
using LTSBackend.Services.Jwt;
using LTSBackend.Services.Permissions;
using LTSBackend.Services.ProfileService;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── FluentValidation ──────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

// ── MediatR ───────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ── JWT Authentication ────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JwtSettings:SecretKey must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// ── Application Services ──────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();

// ── Controllers + JSON ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

// ── Authorization Policies ────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ViewUsers", p => p.Requirements.Add(new PermissionRequirement("ViewUsers")));
    options.AddPolicy("CreateUsers", p => p.Requirements.Add(new PermissionRequirement("CreateUsers")));
    options.AddPolicy("UpdateUsers", p => p.Requirements.Add(new PermissionRequirement("UpdateUsers")));
    options.AddPolicy("DeleteUsers", p => p.Requirements.Add(new PermissionRequirement("DeleteUsers")));
    options.AddPolicy("ManageRoles", p => p.Requirements.Add(new PermissionRequirement("ManageRoles")));
    options.AddPolicy("ViewAuditLogs", p => p.Requirements.Add(new PermissionRequirement("ViewAuditLogs")));
    options.AddPolicy("ViewDashboard", p => p.Requirements.Add(new PermissionRequirement("ViewDashboard")));
});

// ── Swagger ───────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Litigation Tracking System API",
        Version = "v1",
        Description = "LTS Backend API — Vertical Slice Architecture"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// CRITICAL: Middleware order - GlobalExceptionMiddleware FIRST
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LTS API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseStaticFiles();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod());

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("✅ Application Started Successfully!");
app.Run();