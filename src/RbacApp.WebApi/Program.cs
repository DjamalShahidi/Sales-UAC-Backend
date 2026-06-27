using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using RbacApp.Application;
using RbacApp.Infrastructure;
using RbacApp.Infrastructure.MultiTenancy;
using RbacApp.WebApi.Extensions;
using RbacApp.WebApi.Middleware;
using Serilog;

// ---- Serilog ----
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/rbacapp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// ---- Services ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// ---- Swagger ----
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RbacApp API",
        Version = "v1",
        Description = "سیستم مدیریت کاربران و RBAC چندسازمانی"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Operation filters for [RequirePermission] documentation
    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

// ---- CORS (توسعه‌ای) ----
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ---- Authorization ----
builder.Services.AddAuthorization(options =>
    options.AddPermissionPolicies());

// ---- Health Check ----
builder.Services.AddHealthChecks();

var app = builder.Build();

// ---- Middleware Pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

app.UseHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

// Tenant Resolver: قبل از mapping endpoints اجرا می‌شود.
app.UseMiddleware<TenantResolverMiddleware>();

app.MapControllers();

app.Run();
