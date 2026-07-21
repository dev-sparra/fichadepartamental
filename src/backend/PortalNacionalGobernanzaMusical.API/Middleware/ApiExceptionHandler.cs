using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PortalNacionalGobernanzaMusical.API.Middleware;

// Mapea las excepciones de dominio a respuestas RFC 7807 Problem Details para que
// el frontend reciba estados HTTP coherentes (404/403/409) en lugar de 500 genéricos.
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Dictionary<Type, (int StatusCode, string Title)> Mappings = new()
    {
        [typeof(KeyNotFoundException)] = (StatusCodes.Status404NotFound, "Recurso no encontrado"),
        [typeof(UnauthorizedAccessException)] = (StatusCodes.Status403Forbidden, "Acceso denegado"),
        [typeof(InvalidOperationException)] = (StatusCodes.Status409Conflict, "Conflicto de estado"),
        [typeof(ArgumentException)] = (StatusCodes.Status400BadRequest, "Solicitud inválida"),
        // DbUpdateException suele ser violación de FK o restricción única: darle 409
        // con un mensaje claro evita el 500 opaco que confunde al usuario final.
        [typeof(DbUpdateException)] = (StatusCodes.Status409Conflict, "Conflicto de integridad de datos")
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!Mappings.TryGetValue(exception.GetType(), out var mapping))
        {
            return false; // Deja que el handler por defecto gestione los 500 y otros.
        }

        // Importante: devolver 'true' suprime el middleware de diagnósticos para esta
        // excepción, por eso se registra antes de responder.
        logger.LogWarning(exception, "Excepción de API manejada: {Title}", mapping.Title);

        var problem = new ProblemDetails
        {
            Status = mapping.StatusCode,
            Title = mapping.Title,
            Type = $"https://httpstatuses.io/{mapping.StatusCode}",
            Detail = mapping.Title, // Mensaje seguro, no filtra exception.Message
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = mapping.StatusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}