using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using PortalNacionalGobernanzaMusical.API.Middleware;
using PortalNacionalGobernanzaMusical.Application;
using PortalNacionalGobernanzaMusical.Application.Auth;
using PortalNacionalGobernanzaMusical.Infrastructure;
using PortalNacionalGobernanzaMusical.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Health checks: el de base de datos valida conectividad real contra MySQL.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", failureStatus: HealthStatus.Unhealthy, tags: ["db", "mysql"]);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? new JwtSettings();

builder.Services.AddSingleton(jwtSettings);
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        // Variables de entorno pueden venir como string simple (comma-separated).
        var originsSection = builder.Configuration.GetSection("Cors:AllowedOrigins");
        var origins = originsSection.Get<string[]>();
        if (origins is null || origins.Length == 0)
        {
            var raw = originsSection.Value ?? builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty;
            origins = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        Console.WriteLine($"[CORS-DEBUG] originsSection.Value='{originsSection.Value}'");
        Console.WriteLine($"[CORS-DEBUG] builder.Configuration['Cors:AllowedOrigins']='{builder.Configuration["Cors:AllowedOrigins"]}'");
        Console.WriteLine($"[CORS-DEBUG] Parsed origins count={origins.Length}: {string.Join(" | ", origins)}");

        if (origins.Length == 0)
        {
            Console.WriteLine("[CORS-DEBUG] No origins configured -> AllowAnyOrigin (temporal)");
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            // SetIsOriginAllowed: case-insensitive match (browsers lowercase host pero el scheme puede variar)
            policy.SetIsOriginAllowed(host => origins.Any(o => string.Equals(o, host, StringComparison.OrdinalIgnoreCase)))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseHttpsRedirection();

// Servir frontend Angular desde wwwroot en entornos no-Development.
// En Development el frontend corre por separado con `ng serve` en http://localhost:4200.
if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}

// Orden correcto: UseRouting → UseCors → UseAuthentication → UseAuthorization → MapControllers.
// Sin UseRouting explicito, MapControllers inyecta routing DESPUES de UseCors y CORS no matchea endpoint.
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// /health: liveness barato (la API responde). /health/ready: incluye la base de datos
// y reporta detalles para diagnosticar antes de que el frontend intente cargar datos.
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
app.MapControllers();

// SPA fallback para rutas que no son API ni archivos estaticos (Angular router).
if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
