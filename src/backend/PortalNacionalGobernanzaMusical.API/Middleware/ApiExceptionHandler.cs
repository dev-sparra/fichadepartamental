using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalNacionalGobernanzaMusical.Application.Common;

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
        // Las validaciones de captura ya traen mensajes redactados para el usuario: se devuelven
        // con el detalle por campo para que el formulario pueda señalar qué corregir.
        if (exception is DomainValidationException validationException)
        {
            await WriteValidationProblemAsync(httpContext, validationException, cancellationToken);
            return true;
        }

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

    /// <summary>
    /// Responde 400 con el resumen funcional en <c>detalleResumen</c> (el campo que lee el
    /// frontend) y la lista de campos por corregir en <c>errores</c>.
    /// </summary>
    private static async Task WriteValidationProblemAsync(
        HttpContext httpContext,
        DomainValidationException exception,
        CancellationToken cancellationToken)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Datos por corregir",
            Type = "https://httpstatuses.io/400",
            Detail = exception.Summary,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["detalleResumen"] = exception.Summary;
        problem.Extensions["errores"] = exception.Errors
            .Select(error => new { campo = error.Field, etiqueta = error.FieldLabel, mensaje = error.Message })
            .ToArray();

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
}