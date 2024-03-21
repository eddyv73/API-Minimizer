using Microsoft.AspNetCore.Mvc;
using MinimizerCommon.Commons;
using Swashbuckle.AspNetCore.Annotations;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API_Minimizer_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        // GET: api/<StatusController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<StatusController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<StatusController>
        /// <summary>
        /// Represents an action result that returns an HTTP status code and an object to write to the response.
        /// This method is called to check the status of the API. Eddy
        ///
        /// <returns>An IActionResult object.</returns>
        /// <param name="value">The value to check.</param>
        /// <response code="200">Returns the status of the API.</response>
        /// <response code="400">If the item is null.</response>
        /// <response code="500">If the item is null.</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /Todo
        ///     {
        ///        "value": "BackApi"
        ///     }
        ///
        /// </remarks>
        /// <param name="value">The value to check.</param>
        /// <returns>An IActionResult object.</returns>
        ///  </summary>
        [HttpPost]
        [SwaggerResponse(200, "Returns the status of the API.")]
        [SwaggerResponse(400, "If the item is null.")]
        [SwaggerResponse(500, "If the item is null.")]
        [SwaggerOperation(Summary = "This method is called to check the status of the API. Eddy")]
        public IActionResult Post([FromBody] string value)
        {
            if (value == null)
            {
                value = "BackApi";
            }
            var status = new LifeCheck(value, true);
            return Ok(status);
        }

        // PUT api/<StatusController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<StatusController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }


        // add new method to return the time of the server
        [HttpGet("time")]
        public IActionResult GetTime()
        {
            return Ok(DateTime.Now);
        }
        // add new method to return array of times of the server utc and gmt-5
        /// <summary>
        /// Gets the current times.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> representing the HTTP response.</returns>
        [HttpGet("times")]
        public IActionResult GetTimes()
        {
            return Ok(new string[] { DateTime.Now.ToUniversalTime().ToString(), DateTime.Now.AddHours(-5).ToString() });
        }

        // create a new method to return the time zones using the class TimesZones, and acept a object in the query string with timezone required
        public IActionResult GetTimeZones([FromQuery] string timezone)
        {
            var times = new TimesZones();
            if (string.IsNullOrEmpty(timezone))
            {
                return Ok(times.TimeZones);
            }
            else
            {
                return Ok(times.TimeZones.Where(x => x.Name == timezone));
            }
        }
    }
}
