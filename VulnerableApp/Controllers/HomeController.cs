using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Models;

namespace VulnerableApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var sw = Stopwatch.StartNew();
        var user = HttpContext.Session.GetString("User") ?? "anonimo";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation("Inicio Home.Index Usuario:{User} IP:{IP}", user, ip);

        var result = View();

        sw.Stop();
        _logger.LogInformation("Fin Home.Index Usuario:{User} DuracionMs:{DurationMs}", user, sw.ElapsedMilliseconds);
        return result;
    }

    public IActionResult Privacy()
    {
        var sw = Stopwatch.StartNew();
        var user = HttpContext.Session.GetString("User") ?? "anonimo";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        _logger.LogInformation("Inicio Home.Privacy Usuario:{User} IP:{IP}", user, ip);

        var result = View();

        sw.Stop();
        _logger.LogInformation("Fin Home.Privacy Usuario:{User} DuracionMs:{DurationMs}", user, sw.ElapsedMilliseconds);
        return result;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var sw = Stopwatch.StartNew();
        var user = HttpContext.Session.GetString("User") ?? "anonimo";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogWarning("Home.Error mostrado a Usuario:{User} IP:{IP} RequestId:{RequestId}", user, ip, requestId);

        var result = View(new ErrorViewModel { RequestId = requestId });

        sw.Stop();
        _logger.LogInformation("Fin Home.Error Usuario:{User} DuracionMs:{DurationMs}", user, sw.ElapsedMilliseconds);
        return result;
    }
}
