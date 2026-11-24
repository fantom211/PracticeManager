using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PracticeManager.Api.Services;

namespace PracticeManager.Api.Middleware;

/// <summary>
/// Global exception handler. Converts known exceptions to ProblemDetails with a Russian `detail`
/// and a traceId. Unknown exceptions become a generic 500 ProblemDetails.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        var (statusCode, title, detail) = exception switch
        {
            DocumentStatusConflictException ex => (StatusCodes.Status409Conflict, "Конфликт статуса документа", ex.Message),
            FileValidationException ex => (StatusCodes.Status415UnsupportedMediaType, "Недопустимый файл", ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, "Не найдено", ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Некорректный запрос", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", "Произошла непредвиденная ошибка. Попробуйте ещё раз позже.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", traceId);
        }
        else
        {
            logger.LogWarning("Handled exception {ExceptionType}: {Message}. TraceId={TraceId}", exception.GetType().Name, exception.Message, traceId);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Extensions = { ["traceId"] = traceId }
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
