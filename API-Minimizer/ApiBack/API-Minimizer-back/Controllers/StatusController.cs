using Microsoft.AspNetCore.Mvc;
using MinimizerCommon.Commons;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace API_Minimizer_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;
        private readonly IHealthService _healthService;
        private readonly IApiKeyValidationService _apiKeyValidationService;
        private readonly IResponseFormattingService _responseFormattingService;
        private readonly IDbContext _dbContext;
        private readonly IAccountService _accountService;

        public StatusController(
            ILogger<StatusController> logger,
            IHealthService healthService,
            IApiKeyValidationService apiKeyValidationService,
            IResponseFormattingService responseFormattingService,
            IDbContext dbContext,
            IAccountService accountService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
            _apiKeyValidationService = apiKeyValidationService ?? throw new ArgumentNullException(nameof(apiKeyValidationService));
            _responseFormattingService = responseFormattingService ?? throw new ArgumentNullException(nameof(responseFormattingService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        [HttpGet]
        public IEnumerable<string> Get() => new[] { "value1", "value2" };

        [HttpGet("{id}")]
        public string Get(int id) => "value";

        [HttpPost]
        [SwaggerResponse(200, "Returns the status of the API.")]
        [SwaggerResponse(400, "If the item is null or invalid.")]
        [SwaggerResponse(401, "If the request is unauthorized.")]
        [SwaggerResponse(403, "If the request is forbidden.")]
        [SwaggerResponse(500, "If there's a server error.")]
        [SwaggerOperation(Summary = "Checks the status of the API with validation logic.")]
        public async Task<IActionResult> Post([FromBody] string? value, 
            [FromHeader(Name = "X-API-Key")] string? apiKey, 
            [FromQuery] string mode = "standard")
        {
            try
            {
                // Handle null value based on mode
                value = HandleNullValue(value, mode);

                // Validate API key
                if (!_apiKeyValidationService.ValidateApiKey(apiKey, mode, out bool isAdmin))
                {
                    return Unauthorized(new { error = "Valid API key required", code = "ERR002" });
                }

                // Validate value length
                if (!ValidateValueLength(value))
                {
                    return BadRequest(new { error = "Value exceeds maximum length of 50 characters", code = "ERR005" });
                }

                // Process value and extract metadata
                var (environment, responseType) = ProcessValue(value, mode);

                // Perform health checks
                var healthResult = await _healthService.PerformHealthCheckAsync(mode);
                
                if (!healthResult.Success)
                {
                    return StatusCode(500, new { error = "Infrastructure check error", code = "ERR008" });
                }

                // Determine status and prepare response
                var status = _healthService.DetermineStatus(healthResult.HealthScore);
                var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;
                
                var result = _responseFormattingService.PrepareResponse(
                    value, status, healthResult.HealthScore, healthResult.Warnings, 
                    environment, responseType, isAdmin, mode, clientIp, userAgent);

                // Set response headers
                SetResponseHeaders(healthResult.HealthScore, environment, healthResult.Warnings);

                return DetermineHttpResponse(status, mode, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in status check");
                return BadRequest(new { error = ex.Message, code = "ERR001" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in status check");
                return StatusCode(500, new { error = "Internal server error", code = "ERR500" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TransactionRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ErrorResponse { Code = "ERR400", Message = "Request cannot be null" });
            }

            var validationErrors = ValidateTransactionRequest(request);
            if (validationErrors.Any())
            {
                return BadRequest(new ValidationErrorResponse { Code = "ERR422", Message = "Validation failed", Errors = validationErrors });
            }

            var transaction = _dbContext.Transactions.FirstOrDefault(t => t.Id == id);
            if (transaction == null)
            {
                return NotFound(new ErrorResponse { Code = "ERR404", Message = $"Transaction with ID {id} not found" });
            }

            if (!IsAuthorizedToModifyTransaction(transaction))
            {
                return Forbid();
            }

            UpdateTransaction(transaction, request);
            await _dbContext.SaveChangesAsync();
            await _accountService.RecalculateBalanceAsync(transaction.AccountId);

            var categoryName = _dbContext.Categories
                .Where(c => c.Id == transaction.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefault();

            return Ok(new TransactionResponse
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                CategoryId = transaction.CategoryId,
                CategoryName = categoryName,
                UpdatedAt = transaction.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var transaction = _dbContext.Transactions.FirstOrDefault(t => t.Id == id);
                if (transaction == null)
                {
                    return NotFound(new { error = "Resource not found", code = "ERR404" });
                }

                // Note: In a real implementation, you would remove from DbSet
                // _dbContext.Transactions.Remove(transaction);
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resource");
                return StatusCode(500, new { error = "Internal server error", code = "ERR500" });
            }
        }

        [HttpGet("time")]
        public IActionResult GetTime() => Ok(DateTime.Now);

        [HttpGet("times")]
        public IActionResult GetTimes() => Ok(new[] { DateTime.UtcNow.ToString(), DateTime.Now.AddHours(-5).ToString() });

        [HttpGet("timezone")]
        public IActionResult GetTimeZone() => Ok(new TimesZones());

        // Simple helper methods
        private string HandleNullValue(string? value, string mode)
        {
            if (value != null) return value;

            return mode switch
            {
                "strict" => throw new ArgumentException("Value cannot be null in strict mode"),
                "permissive" or "debug" => "BackApi",
                _ => "BackApi"
            };
        }

        private bool ValidateValueLength(string value)
        {
            if (value.Length > 50)
            {
                _logger.LogWarning("Value exceeds maximum length: {Length}", value.Length);
                return false;
            }
            return true;
        }

        private (string environment, string responseType) ProcessValue(string value, string mode)
        {
            string environment = "production";
            string responseType = "standard";

            if (value.Contains("env="))
            {
                var envMatch = Regex.Match(value, @"env=(\w+)");
                if (envMatch.Success)
                {
                    environment = envMatch.Groups[1].Value.ToLower() switch
                    {
                        "dev" or "development" => "development",
                        "test" or "testing" => "testing",
                        "stag" or "staging" => "staging",
                        "prod" or "production" => "production",
                        _ => "production"
                    };
                }
            }

            if (value.Contains("format="))
            {
                var formatMatch = Regex.Match(value, @"format=(\w+)");
                if (formatMatch.Success)
                {
                    responseType = formatMatch.Groups[1].Value.ToLower() switch
                    {
                        "detailed" => "detailed",
                        "minimal" => "minimal",
                        "json" or "xml" => formatMatch.Groups[1].Value.ToLower(),
                        _ => "standard"
                    };
                }
            }

            return (environment, responseType);
        }

        private void SetResponseHeaders(int healthScore, string environment, List<string> warnings)
        {
            Response.Headers.Add("X-Health-Score", healthScore.ToString());
            Response.Headers.Add("X-Environment", environment);

            if (warnings.Any())
            {
                Response.Headers.Add("X-Health-Warnings", string.Join("; ", warnings));
            }
        }

        private IActionResult DetermineHttpResponse(string status, string mode, object result) =>
            status switch
            {
                "Critical" => StatusCode(500, result),
                "Unhealthy" => StatusCode(503, result),
                "Degraded" when mode == "strict" => StatusCode(500, result),
                "Degraded" => Ok(result),
                _ => Ok(result)
            };

        private List<string> ValidateTransactionRequest(TransactionRequest request)
        {
            var errors = new List<string>();

            if (request.Amount <= 0)
            {
                errors.Add("Amount must be greater than zero");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                errors.Add("Description is required");
            }
            else if (request.Description.Length > 200)
            {
                errors.Add("Description cannot exceed 200 characters");
            }

            if (request.TransactionDate > DateTime.UtcNow)
            {
                errors.Add("Transaction date cannot be in the future");
            }

            return errors;
        }

        private bool IsAuthorizedToModifyTransaction(Transaction transaction)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return transaction.UserId == userId || User.IsInRole("Admin");
        }

        private void UpdateTransaction(Transaction transaction, TransactionRequest request)
        {
            transaction.Amount = request.Amount;
            transaction.Description = request.Description;
            transaction.TransactionDate = request.TransactionDate;
            transaction.CategoryId = request.CategoryId;
            transaction.IsRecurring = request.IsRecurring;
            transaction.Tags = request.Tags;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (request.Type == TransactionType.Expense)
            {
                transaction.Amount = -Math.Abs(transaction.Amount);
            }
        }
    }
}
