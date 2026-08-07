using Serilog.Context;

namespace VulnerableApp.Middleware;

/// <summary>
/// Genera (o reutiliza, si el cliente ya envio uno) un identificador unico por
/// peticion y lo expone de tres formas:
///  1) Como header de respuesta 'X-Correlation-ID' (para el cliente/QA).
///  2) Como propiedad ambiental de Serilog (LogContext), para que TODOS los
///     logs generados durante esta peticion -incluyendo los de los
///     controladores ya instrumentados- lo incluyan automaticamente sin
///     tener que modificar cada linea de log.
///  3) En HttpContext.Items, por si algun componente lo necesita explicitamente.
/// Debe registrarse como el PRIMER middleware del pipeline.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
