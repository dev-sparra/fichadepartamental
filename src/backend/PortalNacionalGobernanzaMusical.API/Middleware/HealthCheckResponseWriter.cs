using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PortalNacionalGobernanzaMusical.API.Middleware;

// Serializa el reporte de health checks como JSON legible para que operaciones y el
// frontend puedan diagnosticar el estado de cada verificador (p. ej. "database").
internal static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description,
                exception = entry.Value.Exception?.Message
            })
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, payload);
    }
}