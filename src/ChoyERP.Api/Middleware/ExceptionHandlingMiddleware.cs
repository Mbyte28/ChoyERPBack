using ChoyERP.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Api.Middleware;

/// <summary>
/// Traduce excepciones a respuestas ProblemDetails consistentes, sin exponer detalles internos.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationAppException exception)
        {
            await WriteProblemAsync(
                context,
                exception.StatusCode,
                "Datos inválidos",
                exception.Message,
                exception.Errors);
        }
        catch (AppException exception)
        {
            await WriteProblemAsync(context, exception.StatusCode, "No se pudo completar la operación", exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Error de persistencia en {Path}", context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflicto de datos",
                "La operación entra en conflicto con datos existentes o referencias inválidas.");
        }
        catch (BadHttpRequestException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ocurrió un error interno. Consulte el identificador de traza con el equipo técnico.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
