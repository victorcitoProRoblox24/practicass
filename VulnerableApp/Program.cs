using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using VulnerableApp.Data;
using VulnerableApp.Middleware;

// Bootstrap logger: captura cualquier error que ocurra ANTES de que la
// configuracion completa (appsettings.json) este disponible, por ejemplo
// un fallo al construir el WebApplicationBuilder.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Iniciando VulnerableApp...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Reemplaza el logger de bootstrap por el pipeline completo definido
    // en appsettings.json (Serilog: MinimumLevel, WriteTo, Enrich).
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddSession();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        // El HSTS es una cabecera de seguridad de transporte, no forma parte
        // del manejo de excepciones: se conserva para produccion.
        app.UseHsts();
    }

    // 1) CorrelationId: debe ir primero para que TODO lo que se registre
    //    despues (incluido dentro de RequestLogging/ExceptionHandling y de
    //    los controladores) quede etiquetado con el mismo identificador.
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseHttpsRedirection();

    // 2) RequestLogging envuelve a ExceptionHandling para poder registrar el
    //    codigo de estado FINAL (incluye el 500 que pone ExceptionHandling)
    //    y el tiempo total de la peticion.
    app.UseMiddleware<RequestLoggingMiddleware>();

    // 3) ExceptionHandling: red de seguridad global para cualquier excepcion
    //    no controlada localmente por un controlador. Reemplaza al
    //    UseExceptionHandler("/Home/Error") por defecto para poder loguear
    //    con CorrelationId y devolver un JSON consistente.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseRouting();

    app.UseSession();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException se excluye a proposito: la herramienta 'dotnet ef'
    // aborta el host intencionalmente para leer los servicios configurados
    // (p.ej. 'dotnet ef database update'), eso no es un error real de arranque.
    Log.Fatal(ex, "VulnerableApp termino inesperadamente durante el arranque");
}
finally
{
    Log.CloseAndFlush();
}
