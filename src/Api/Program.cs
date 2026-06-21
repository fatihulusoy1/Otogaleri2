using System.Text;
using AutoGallerySaaS.Api.Middleware;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Admin.Services;
using AutoGallerySaaS.Application.Features.Auth.Services;
using AutoGallerySaaS.Application.Features.Consignments.Services;
using AutoGallerySaaS.Application.Features.Crm.Services;
using AutoGallerySaaS.Application.Features.Dashboard.Services;
using AutoGallerySaaS.Application.Features.Finance.Services;
using AutoGallerySaaS.Application.Features.Vehicles.Services;
using AutoGallerySaaS.Infrastructure.Authentication;
using AutoGallerySaaS.Infrastructure.Services;
using AutoGallerySaaS.Infrastructure.Storage;
using AutoGallerySaaS.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoGallery SaaS API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token girin. Ornek: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IDateTime, DateTimeService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IConsignmentService, ConsignmentService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<ICrmService, CrmService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=autogallery_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_1234567890123456";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AutoGallerySaaS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AutoGallerySaaS.Client";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireClaim("is_super_admin", bool.TrueString));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var tenantService = context.RequestServices.GetRequiredService<ITenantService>();
    var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();

    if (currentUserService.TenantId.HasValue)
    {
        tenantService.SetTenantId(currentUserService.TenantId.Value);
    }

    await next();
});
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    message = "AutoGallery SaaS API is running",
    environment = app.Environment.EnvironmentName
}));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await DbInitializer.SeedAsync(dbContext);
}

app.Run();
