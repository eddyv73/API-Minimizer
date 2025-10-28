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
        /// <summary>
        /// Checks the status of the API and returns a LifeCheck result.
        /// If the request body <paramref name="value"/> is null, the name defaults to "BackApi".
        /// </summary>
        /// <param name="value">Optional string read from the request body to identify the status check; may be null.</param>
        /// <returns>HTTP 200 (OK) containing a LifeCheck object describing the API status.</returns>
        /// <response code="200">Returns the LifeCheck status object.</response>
        /// <response code="400">Bad request — input was null or invalid (declared in Swagger metadata).</response>
        /// <response code="500">Internal server error (declared in Swagger metadata).</response>
        /// <remarks>The returned LifeCheck is constructed with the provided or default name and has its Status set to false.</remarks>
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

            var status = new LifeCheck(value, true)
            {
                Status = false
            };
            return Ok(status);
        }

        // PUT api/<StatusController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
            DateTime currentDate = DateTime.Now;
        }

        // DELETE api/<StatusController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        // GET api/<StatusController>/currentdate
        [HttpGet("currentdate")]
        public string GetCurrentDate()
        {
            DateTime currentDate = DateTime.Now;
            return currentDate.ToString();
        }
    }
}
