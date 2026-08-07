using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Data;

namespace VulnerableApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext db, ILogger<AuthController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Login()
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("Inicio Auth.Login (GET) IP:{IP}", ip);

            var result = View();

            sw.Stop();
            _logger.LogInformation("Fin Auth.Login (GET) DuracionMs:{DurationMs}", sw.ElapsedMilliseconds);
            return result;
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // IMPORTANTE: nunca se registra 'password' en ningun log, solo el nombre de usuario.
            _logger.LogInformation(
                "Inicio Auth.Login (POST) Usuario:{User} IP:{IP}", username, ip);

            // Gatillo de prueba para SEGG-U2-P3G-5 (excepcion NO controlada):
            // se lanza fuera de cualquier try/catch local.
            if (username == "__UNCONTROLLED_EXCEPTION_TEST__")
            {
                throw new InvalidOperationException("Excepcion de prueba NO controlada (Auth.Login)");
            }

            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                _logger.LogDebug("Verificando hash BCrypt para Usuario:{User} (password nunca se registra en logs)", username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning(
                        "AuthEvent:LoginFailed Usuario:{User} IP:{IP} Motivo:CredencialesInvalidas",
                        username, ip);

                    ViewBag.Error = "Credenciales inválidas";
                    return View();
                }

                HttpContext.Session.SetString("User", user.Username);
                HttpContext.Session.SetInt32("UserId", user.Id);

                sw.Stop();
                _logger.LogInformation(
                    "AuthEvent:LoginSuccess Usuario:{User} IP:{IP} DuracionMs:{DurationMs}",
                    user.Username, ip, sw.ElapsedMilliseconds);

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Auth.Login (POST) Usuario:{User} IP:{IP}", username, ip);
                throw;
            }
        }

        public IActionResult Dashboard()
        {
            var sw = Stopwatch.StartNew();
            var user = HttpContext.Session.GetString("User") ?? "anonimo";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("Inicio Auth.Dashboard Usuario:{User} IP:{IP}", user, ip);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("AuthEvent:AccesoNoAutenticado a Dashboard IP:{IP}", ip);
                return RedirectToAction("Login");
            }

            try
            {
                var dbUser = _db.Users.Find(userId.Value);

                sw.Stop();
                _logger.LogInformation(
                    "Fin Auth.Dashboard Usuario:{User} DuracionMs:{DurationMs}", user, sw.ElapsedMilliseconds);

                return View(dbUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Auth.Dashboard Usuario:{User} IP:{IP}", user, ip);
                throw;
            }
        }

        public IActionResult Logout()
        {
            var user = HttpContext.Session.GetString("User") ?? "anonimo";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("AuthEvent:Logout Usuario:{User} IP:{IP}", user, ip);

            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
