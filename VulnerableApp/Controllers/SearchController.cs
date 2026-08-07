using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Data;
using VulnerableApp.Models;

namespace VulnerableApp.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SearchController> _logger;

        public SearchController(AppDbContext db, ILogger<SearchController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index(string search)
        {
            var sw = Stopwatch.StartNew();
            var user = HttpContext.Session.GetString("User") ?? "anonimo";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Inicio Search.Index Usuario:{User} IP:{IP} Parametros:{@Parametros}",
                user, ip, new { search });

            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    _logger.LogInformation("Search.Index sin termino de busqueda, Usuario:{User}", user);
                    return View(new List<User>());
                }

                // Gatillo de prueba para SEGG-U2-P3G-5 (excepcion CONTROLADA):
                // se captura localmente y la peticion sigue respondiendo con
                // normalidad (200), a diferencia de una excepcion no controlada.
                if (search == "__CONTROLLED_EXCEPTION_TEST__")
                {
                    try
                    {
                        throw new ArgumentException("Termino de busqueda de prueba invalido (excepcion controlada)");
                    }
                    catch (ArgumentException exControlled)
                    {
                        _logger.LogError(exControlled,
                            "Excepcion controlada capturada en Search.Index Usuario:{User} IP:{IP}", user, ip);
                        ViewBag.Error = "Termino de busqueda invalido.";
                        return View(new List<User>());
                    }
                }

                _logger.LogDebug("Ejecutando consulta parametrizada Users.Where(Username contains {Search})", search);

                var users = _db.Users
                    .Where(u => u.Username.Contains(search))
                    .ToList();

                sw.Stop();
                _logger.LogInformation(
                    "Fin Search.Index Usuario:{User} Resultados:{ResultCount} DuracionMs:{DurationMs}",
                    user, users.Count, sw.ElapsedMilliseconds);

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error en Search.Index Usuario:{User} IP:{IP} Parametros:{@Parametros}",
                    user, ip, new { search });
                throw;
            }
        }
    }
}
