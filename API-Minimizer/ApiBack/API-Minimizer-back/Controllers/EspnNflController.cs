using Microsoft.AspNetCore.Mvc;
using API_Minimizer_back.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace API_Minimizer_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EspnNflController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EspnNflController> _logger;

        public EspnNflController(IHttpClientFactory httpClientFactory, ILogger<EspnNflController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Gets NFL scoreboard data from ESPN API for a specific year.
        /// </summary>
        /// <param name="year">The year for which to retrieve NFL scoreboard data (e.g., 2023). Defaults to 2023.</param>
        /// <param name="limit">The maximum number of results to return. Defaults to 1000.</param>
        /// <returns>NFL scoreboard data from ESPN API</returns>
        /// <response code="200">Returns the NFL scoreboard data</response>
        /// <response code="500">If there was an error retrieving the data</response>
        [HttpGet]
        [SwaggerResponse(200, "Returns NFL scoreboard data from ESPN API")]
        [SwaggerResponse(500, "Internal server error")]
        [SwaggerOperation(Summary = "Get NFL scoreboard data from ESPN API", Description = "Retrieves NFL scoreboard data from ESPN API for a specific year")]
        public async Task<IActionResult> GetNflScoreboard([FromQuery] int year = 2023, [FromQuery] int limit = 1000)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?limit={limit}&dates={year}";
                
                _logger.LogInformation($"Fetching NFL scoreboard data from ESPN API: {url}");
                
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"ESPN API returned status code: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, $"Error fetching data from ESPN API: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                
                // Parse the JSON to validate it
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var scoreboardData = JsonSerializer.Deserialize<EspnScoreboardResponse>(content, options);
                
                return Ok(scoreboardData);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while fetching ESPN NFL data");
                return StatusCode(500, $"Error connecting to ESPN API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing ESPN API response");
                return StatusCode(500, $"Error parsing ESPN API response: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching ESPN NFL data");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets NFL scoreboard data from ESPN API for a specific date range.
        /// </summary>
        /// <param name="startDate">Start date in YYYYMMDD format</param>
        /// <param name="endDate">End date in YYYYMMDD format</param>
        /// <param name="limit">The maximum number of results to return. Defaults to 1000.</param>
        /// <returns>NFL scoreboard data from ESPN API</returns>
        /// <response code="200">Returns the NFL scoreboard data</response>
        /// <response code="400">If the date format is invalid</response>
        /// <response code="500">If there was an error retrieving the data</response>
        [HttpGet("daterange")]
        [SwaggerResponse(200, "Returns NFL scoreboard data from ESPN API")]
        [SwaggerResponse(400, "Invalid date format")]
        [SwaggerResponse(500, "Internal server error")]
        [SwaggerOperation(Summary = "Get NFL scoreboard data by date range", Description = "Retrieves NFL scoreboard data from ESPN API for a specific date range")]
        public async Task<IActionResult> GetNflScoreboardByDateRange([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int limit = 1000)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?limit={limit}&dates={startDate}-{endDate}";
                
                _logger.LogInformation($"Fetching NFL scoreboard data from ESPN API: {url}");
                
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"ESPN API returned status code: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, $"Error fetching data from ESPN API: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var scoreboardData = JsonSerializer.Deserialize<EspnScoreboardResponse>(content, options);
                
                return Ok(scoreboardData);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while fetching ESPN NFL data");
                return StatusCode(500, $"Error connecting to ESPN API: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing ESPN API response");
                return StatusCode(500, $"Error parsing ESPN API response: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching ESPN NFL data");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }
    }
}
