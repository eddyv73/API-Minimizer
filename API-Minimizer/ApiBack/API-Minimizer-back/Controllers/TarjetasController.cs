using Microsoft.AspNetCore.Mvc;
using BancoApp;

namespace API_Minimizer_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
    {
        private static List<Tarjeta> _tarjetas = new List<Tarjeta>();

        [HttpGet]
        public ActionResult<IEnumerable<Tarjeta>> GetTarjetas()
        {
            return Ok(_tarjetas);
        }

        [HttpGet("{numeroTarjeta}")]
        public ActionResult<Tarjeta> GetTarjeta(string numeroTarjeta)
        {
            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound();
            }
            return Ok(tarjeta);
        }

        [HttpPost]
        public ActionResult<Tarjeta> CrearTarjeta(Tarjeta tarjeta)
        {
            tarjeta.EnmascararNumero();
            _tarjetas.Add(tarjeta);
            return CreatedAtAction(nameof(GetTarjeta), new { numeroTarjeta = tarjeta.NumeroTarjeta }, tarjeta);
        }

        [HttpPut("{numeroTarjeta}")]
        public IActionResult ActualizarTarjeta(string numeroTarjeta, Tarjeta tarjetaActualizada)
        {
            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound();
            }

            // Actualizar propiedades
            tarjeta.TipoTarjeta = tarjetaActualizada.TipoTarjeta;
            tarjeta.EstaActiva = tarjetaActualizada.EstaActiva;
            tarjeta.EstaBloqueada = tarjetaActualizada.EstaBloqueada;
            tarjeta.MotivoBloqueo = tarjetaActualizada.MotivoBloqueo;
            tarjeta.LimiteCredito = tarjetaActualizada.LimiteCredito;
            tarjeta.SaldoActual = tarjetaActualizada.SaldoActual;
            tarjeta.CalcularDisponible();

            return NoContent();
        }

        [HttpDelete("{numeroTarjeta}")]
        public IActionResult EliminarTarjeta(string numeroTarjeta)
        {
            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound();
            }

            _tarjetas.Remove(tarjeta);
            return NoContent();
        }
    }
} 