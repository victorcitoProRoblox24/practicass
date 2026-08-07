using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VulnerableApp.Controllers
{
    public class CommentController : Controller
    {
        private static List<string> _comments = new();
        private readonly ILogger<CommentController> _logger;

        public CommentController(ILogger<CommentController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var sw = Stopwatch.StartNew();
            var user = HttpContext.Session.GetString("User") ?? "anonimo";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("Inicio Comment.Index Usuario:{User} IP:{IP}", user, ip);

            var result = View(_comments);

            sw.Stop();
            _logger.LogInformation(
                "Fin Comment.Index Usuario:{User} TotalComentarios:{Count} DuracionMs:{DurationMs}",
                user, _comments.Count, sw.ElapsedMilliseconds);

            return result;
        }

        [HttpPost]
        public IActionResult AddComment(string comment)
        {
            var sw = Stopwatch.StartNew();
            var user = HttpContext.Session.GetString("User") ?? "anonimo";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Inicio Comment.AddComment Usuario:{User} IP:{IP} Parametros:{@Parametros}",
                user, ip, new { comment });

            // Gatillo de prueba para SEGG-U2-P3G-5 (excepcion NO controlada):
            // se lanza fuera de cualquier try/catch local, por lo que debe
            // ser capturada por ExceptionHandlingMiddleware (500 JSON).
            if (comment == "__UNCONTROLLED_EXCEPTION_TEST__")
            {
                throw new InvalidOperationException("Excepcion de prueba NO controlada (Comment.AddComment)");
            }

            try
            {
                if (!string.IsNullOrEmpty(comment))
                {
                    _comments.Add(comment);
                    _logger.LogInformation("Comentario agregado por Usuario:{User}", user);
                }
                else
                {
                    _logger.LogWarning("Intento de agregar comentario vacio, Usuario:{User} IP:{IP}", user, ip);
                }

                sw.Stop();
                _logger.LogInformation(
                    "Fin Comment.AddComment Usuario:{User} DuracionMs:{DurationMs}", user, sw.ElapsedMilliseconds);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Comment.AddComment Usuario:{User} IP:{IP}", user, ip);
                throw;
            }
        }
    }
}
