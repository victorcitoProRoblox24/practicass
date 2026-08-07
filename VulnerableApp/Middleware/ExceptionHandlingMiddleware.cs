namespace VulnerableApp.Middleware;

/// <summary>
/// Red de seguridad global: captura cualquier excepcion NO controlada que
/// haya escapado de los try/catch locales de los controladores (o de
/// cualquier otro componente del pipeline), la registra con LogError junto
/// con el CorrelationId de la peticion (disponible via LogContext gracias a
/// CorrelationIdMiddleware) y responde 500 sin filtrar detalles internos
/// (stack trace, mensajes de motor de BD, etc.) al cliente.
/// Debe registrarse ANTES de UseRouting/MapControllers para envolver toda
/// la ejecucion de la peticion.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue("X-Correlation-ID", out var cid) ? cid?.ToString() : null;

            _logger.LogError(ex,
                "Unhandled exception. Metodo:{Method} Ruta:{Path} CorrelationId:{CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Ocurrio un error inesperado.",
                    correlationId
                });
            }
        }
    }
}
