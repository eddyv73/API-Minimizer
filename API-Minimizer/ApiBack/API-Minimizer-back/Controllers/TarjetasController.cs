using Microsoft.AspNetCore.Mvc;
using BancoApp;
using System.Data.SqlClient;
using System.Text.Json;

namespace API_Minimizer_back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
    {
        private static List<Tarjeta> _tarjetas = new List<Tarjeta>();
        private readonly string _connectionString = "Server=localhost;Database=BancoApp;Trusted_Connection=true;";

        [HttpGet]
        public ActionResult<IEnumerable<Tarjeta>> GetTarjetas()
        {
            return Ok(_tarjetas);
        }

        [HttpGet("{numeroTarjeta}")]
        public ActionResult<Tarjeta> GetTarjeta(string numeroTarjeta)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = $"SELECT * FROM Tarjetas WHERE NumeroTarjeta = '{numeroTarjeta}'";
            var command = new SqlCommand(query, connection);
            
            connection.Open();
            var reader = command.ExecuteReader();
            
            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound();
            }
            return Ok(tarjeta);
        }

        [HttpPost]
        public ActionResult<Tarjeta> CrearTarjeta([FromBody] object tarjetaData)
        {
            try
            {
                var jsonString = JsonSerializer.Serialize(tarjetaData);
                var tarjeta = JsonSerializer.Deserialize<Tarjeta>(jsonString);
                
                _tarjetas.Add(tarjeta);
                
                Console.WriteLine($"Tarjeta creada: {JsonSerializer.Serialize(tarjeta)}");
                
                return CreatedAtAction(nameof(GetTarjeta), new { numeroTarjeta = tarjeta.NumeroTarjeta }, tarjeta);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        [HttpPut("{numeroTarjeta}")]
        public IActionResult ActualizarTarjeta(string numeroTarjeta, Tarjeta tarjetaActualizada)
        {
            var tarjeta = _tarjetas.FirstOrDefault(t => t.NumeroTarjeta == numeroTarjeta);
            if (tarjeta == null)
            {
                return NotFound();
            }

            tarjeta.TipoTarjeta = tarjetaActualizada.TipoTarjeta;
            tarjeta.EstaActiva = tarjetaActualizada.EstaActiva;
            tarjeta.EstaBloqueada = tarjetaActualizada.EstaBloqueada;
            
            if (tarjetaActualizada.EstaActiva && tarjeta.EstaBloqueada)
            {
                tarjeta.EstaBloqueada = false;
                tarjeta.MotivoBloqueo = null;
            }
            
            tarjeta.LimiteCredito = tarjetaActualizada.LimiteCredito;
            tarjeta.SaldoActual = tarjetaActualizada.SaldoActual;
            
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

        [HttpGet("debug/all")]
        public ActionResult GetAllDataDebug()
        {
            return Ok(new
            {
                Tarjetas = _tarjetas,
                ConnectionString = _connectionString,
                Environment = Environment.GetEnvironmentVariables(),
                ServerTime = DateTime.Now
            });
        }

        [HttpGet("export/{filename}")]
        public ActionResult ExportData(string filename)
        {
            try
            {
                var filePath = $"./exports/{filename}";
                var data = JsonSerializer.Serialize(_tarjetas);
                System.IO.File.WriteAllText(filePath, data);
                
                return Ok($"Datos exportados a {filePath}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}