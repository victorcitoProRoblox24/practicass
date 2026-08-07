using System.Diagnostics;

namespace VulnerableApp.Middleware;

/// <summary>
/// Registra un unico evento por peticion HTTP con metodo, ruta, codigo de
/// respuesta y tiempo de ejecucion en milisegundos. Se registra DESPUES de
/// CorrelationIdMiddleware (para heredar el CorrelationId) pero ENVOLVIENDO
/// a ExceptionHandlingMiddleware, de forma que el codigo de estado que se
/// registra sea siempre el final (incluye el 500 que pone
/// ExceptionHandlingMiddleware cuando algo falla).
/// El nivel del log sube automaticamente a Warning para respuestas 4xx/5xx.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var elapsedMs = sw.Elapsed.TotalMilliseconds;

            if (statusCode >= 500)
            {
                _logger.LogError(
                    "HTTP {Method} {Path} respondio {StatusCode} en {ElapsedMs} ms",
                    method, path, statusCode, elapsedMs);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Path} respondio {StatusCode} en {ElapsedMs} ms",
                    method, path, statusCode, elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} respondio {StatusCode} en {ElapsedMs} ms",
                    method, path, statusCode, elapsedMs);
            }
        }
    }
}
