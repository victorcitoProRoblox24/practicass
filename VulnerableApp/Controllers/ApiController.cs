using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VulnerableApp.Data;

namespace VulnerableApp.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ApiController> _logger;

        public ApiController(AppDbContext db, ILogger<ApiController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("user/{id}")]
        public IActionResult GetUser(int id)
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            _logger.LogInformation(
                "Inicio Api.GetUser IP:{IP} Parametros:{@Parametros}", ip, new { id });

            try
            {
                if (!currentUserId.HasValue)
                {
                    _logger.LogWarning("AuthEvent:AccesoNoAutenticado a Api.GetUser IP:{IP} Parametros:{@Parametros}", ip, new { id });
                    return Unauthorized();
                }

                // Gatillo de prueba para SEGG-U2-P3G-5 (excepcion CONTROLADA
                // en la API): se captura localmente y responde 400, sin
                // llegar al ExceptionHandlingMiddleware.
                if (id == -999)
                {
                    try
                    {
                        throw new ArgumentOutOfRangeException(nameof(id), "Id de prueba invalido (excepcion controlada)");
                    }
                    catch (ArgumentOutOfRangeException exControlled)
                    {
                        _logger.LogError(exControlled, "Excepcion controlada capturada en Api.GetUser IP:{IP}", ip);
                        return BadRequest(new { error = "Id invalido" });
                    }
                }

                _logger.LogDebug(
                    "Comparando UsuarioId solicitado:{RequestedId} con UsuarioId de sesion:{CurrentUserId}",
                    id, currentUserId.Value);

                if (id != currentUserId.Value)
                {
                    _logger.LogWarning(
                        "AuthEvent:AccesoNoAutorizado UsuarioId:{CurrentUserId} intento acceder a UsuarioId:{RequestedId} IP:{IP}",
                        currentUserId.Value, id, ip);
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                var user = _db.Users.Find(id);
                if (user == null)
                {
                    _logger.LogWarning("Api.GetUser UsuarioId:{Id} no encontrado, IP:{IP}", id, ip);
                    return NotFound();
                }

                sw.Stop();
                _logger.LogInformation(
                    "Fin Api.GetUser UsuarioId:{Id} DuracionMs:{DurationMs}", id, sw.ElapsedMilliseconds);

                return Ok(new { user.Id, user.Username, user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Api.GetUser IP:{IP} Parametros:{@Parametros}", ip, new { id });
                throw;
            }
        }

        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var sw = Stopwatch.StartNew();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            _logger.LogInformation("Inicio Api.GetAllUsers IP:{IP}", ip);

            try
            {
                if (!currentUserId.HasValue)
                {
                    _logger.LogWarning("AuthEvent:AccesoNoAutenticado a Api.GetAllUsers IP:{IP}", ip);
                    return Unauthorized();
                }

                var users = _db.Users
                    .Select(u => new { u.Id, u.Username, u.Email })
                    .ToList();

                sw.Stop();
                _logger.LogInformation(
                    "Fin Api.GetAllUsers UsuarioId:{CurrentUserId} Resultados:{ResultCount} DuracionMs:{DurationMs}",
                    currentUserId.Value, users.Count, sw.ElapsedMilliseconds);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Api.GetAllUsers IP:{IP}", ip);
                throw;
            }
        }
    }
}
