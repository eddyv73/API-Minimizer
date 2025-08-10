using Microsoft.AspNetCore.Mvc;
using BancoApp;
using System.Text.Json;

namespace API_Minimizer_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
    {
        private static readonly List<Tarjeta> _tarjetas = new();
        private readonly ILogger<TarjetasController> _logger;

        public TarjetasController(ILogger<TarjetasController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public ActionResult<IEnumerable<Tarjeta>> GetTarjetas()
        {
            _logger.LogInformation("Getting all tarjetas, count: {Count}", _tarjetas.Count);
            return Ok(_tarjetas);
        }

        [HttpGet("{numeroTarjeta}")]
        public ActionResult<Tarjeta> GetTarjeta(string numeroTarjeta)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta))
            {
                return BadRequest("Número de tarjeta es requerido");
            }

            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                _logger.LogWarning("Tarjeta not found: {NumeroTarjeta}", numeroTarjeta);
                return NotFound($"Tarjeta {numeroTarjeta} no encontrada");
            }

            _logger.LogInformation("Retrieved tarjeta: {NumeroTarjeta}", numeroTarjeta);
            return Ok(tarjeta);
        }

        [HttpPost]
        public ActionResult<Tarjeta> CrearTarjeta([FromBody] object tarjetaData)
        {
            if (tarjetaData == null)
            {
                return BadRequest("Datos de tarjeta son requeridos");
            }

            try
            {
                var jsonString = JsonSerializer.Serialize(tarjetaData);
                var tarjeta = JsonSerializer.Deserialize<Tarjeta>(jsonString);
                
                if (tarjeta == null)
                {
                    return BadRequest("Datos de tarjeta inválidos");
                }

                var validationResult = ValidateTarjeta(tarjeta);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.Errors);
                }

                tarjeta.EnmascararNumero();
                tarjeta.CalcularDisponible();
                
                _tarjetas.Add(tarjeta);
                
                _logger.LogInformation("Tarjeta creada successfully: {NumeroTarjeta}", tarjeta.NumeroTarjetaEnmascarado);
                
                return CreatedAtAction(nameof(GetTarjeta), new { numeroTarjeta = tarjeta.NumeroTarjeta }, tarjeta);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing tarjeta data");
                return BadRequest("Formato de datos inválido");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating tarjeta");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{numeroTarjeta}")]
        public IActionResult ActualizarTarjeta(string numeroTarjeta, [FromBody] Tarjeta tarjetaActualizada)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta))
            {
                return BadRequest("Número de tarjeta es requerido");
            }

            if (tarjetaActualizada == null)
            {
                return BadRequest("Datos de tarjeta son requeridos");
            }

            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound($"Tarjeta {numeroTarjeta} no encontrada");
            }

            var validationResult = ValidateTarjetaUpdate(tarjetaActualizada);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            UpdateTarjetaProperties(tarjeta, tarjetaActualizada);
            
            _logger.LogInformation("Tarjeta updated successfully: {NumeroTarjeta}", numeroTarjeta);
            
            return NoContent();
        }

        [HttpDelete("{numeroTarjeta}")]
        public IActionResult EliminarTarjeta(string numeroTarjeta)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta))
            {
                return BadRequest("Número de tarjeta es requerido");
            }

            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound($"Tarjeta {numeroTarjeta} no encontrada");
            }

            _tarjetas.Remove(tarjeta);
            
            _logger.LogInformation("Tarjeta deleted successfully: {NumeroTarjeta}", numeroTarjeta);
            
            return NoContent();
        }

        [HttpGet("health")]
        public ActionResult GetHealthStatus()
        {
            return Ok(new
            {
                Status = "Healthy",
                TotalTarjetas = _tarjetas.Count,
                ServerTime = DateTime.Now,
                Version = "1.0.0"
            });
        }

        // Private helper methods
        private ValidationResult ValidateTarjeta(Tarjeta tarjeta)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(tarjeta.NumeroTarjeta))
                errors.Add("Número de tarjeta es requerido");
            else if (tarjeta.NumeroTarjeta.Length != 16 || !tarjeta.NumeroTarjeta.All(char.IsDigit))
                errors.Add("Número de tarjeta debe tener 16 dígitos");

            if (string.IsNullOrWhiteSpace(tarjeta.NombreTitular))
                errors.Add("Nombre del titular es requerido");

            if (string.IsNullOrWhiteSpace(tarjeta.TipoTarjeta))
                errors.Add("Tipo de tarjeta es requerido");

            if (_tarjetas.Any(t => t.NumeroTarjeta == tarjeta.NumeroTarjeta))
                errors.Add("Ya existe una tarjeta con este número");

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        private ValidationResult ValidateTarjetaUpdate(Tarjeta tarjeta)
        {
            var errors = new List<string>();

            if (tarjeta.LimiteCredito < 0)
                errors.Add("Límite de crédito no puede ser negativo");

            if (tarjeta.SaldoActual < 0)
                errors.Add("Saldo actual no puede ser negativo");

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        private static void UpdateTarjetaProperties(Tarjeta target, Tarjeta source)
        {
            target.TipoTarjeta = source.TipoTarjeta;
            target.EstaActiva = source.EstaActiva;
            target.EstaBloqueada = source.EstaBloqueada;
            
            // Business logic: if activating a blocked card, unblock it
            if (source.EstaActiva && target.EstaBloqueada)
            {
                target.EstaBloqueada = false;
                target.MotivoBloqueo = null;
            }
            
            target.LimiteCredito = source.LimiteCredito;
            target.SaldoActual = source.SaldoActual;
            target.CalcularDisponible();
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
        }
    }
}