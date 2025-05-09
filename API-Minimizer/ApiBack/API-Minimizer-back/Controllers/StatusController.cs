using Microsoft.AspNetCore.Mvc;
using MinimizerCommon.Commons;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.RegularExpressions;

namespace API_Minimizer_back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;
        private readonly DbContext _dbContext;
        private readonly IBudgetService _budgetService;
        private readonly INotificationService _notificationService;
        private readonly IAccountService _accountService;

        public StatusController(
            ILogger<StatusController> logger,
            DbContext dbContext,
            IBudgetService budgetService,
            INotificationService notificationService,
            IAccountService accountService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _budgetService = budgetService;
            _notificationService = notificationService;
            _accountService = accountService;
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
        [SwaggerOperation(Summary = "Checks the status of the API with complex validation logic.")]
        public IActionResult Post([FromBody] string value, [FromHeader(Name = "X-API-Key")] string apiKey, [FromQuery] string mode = "standard")
        {
            value = HandleNullValue(value, mode);
            if (!ValidateApiKey(apiKey, mode, out bool isAdmin))
            {
                return Unauthorized(new { error = "Valid API key required", code = "ERR002" });
            }

            if (!ValidateValueLength(value))
            {
                return BadRequest(new { error = "Value exceeds maximum length of 50 characters", code = "ERR005" });
            }

            var (environment, responseType, healthScore, warnings) = ProcessValue(value, mode);

            if (!PerformInfrastructureChecks(mode, ref healthScore, warnings))
            {
                return StatusCode(500, new { error = "Infrastructure check error", code = "ERR008" });
            }

            var status = DetermineStatus(healthScore);
            var result = PrepareResponse(value, status, healthScore, warnings, environment, responseType, isAdmin, mode);

            SetResponseHeaders(healthScore, environment, warnings);

            return DetermineHttpResponse(status, mode, result);
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

            var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id);
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

            return Ok(new TransactionResponse
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description,
                TransactionDate = transaction.TransactionDate,
                CategoryId = transaction.CategoryId,
                CategoryName = await _dbContext.Categories.Where(c => c.Id == transaction.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(),
                UpdatedAt = transaction.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var transaction = await _dbContext.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    return NotFound(new { error = "Resource not found", code = "ERR404" });
                }

                _dbContext.Transactions.Remove(transaction);
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

        private string HandleNullValue(string value, string mode)
        {
            if (value != null) return value;

            switch (mode)
            {
                case "strict":
                    _logger.LogWarning("Null value received in strict mode");
                    throw new ArgumentException("Value cannot be null in strict mode");
                case "permissive":
                    _logger.LogInformation("Null value in permissive mode, using default");
                    return "BackApi";
                case "debug":
                    _logger.LogDebug("Debug mode detected with null value");
                    return "BackApi";
                default:
                    return "BackApi";
            }
        }

        private bool ValidateApiKey(string apiKey, string mode, out bool isAdmin)
        {
            isAdmin = false;
            if (string.IsNullOrEmpty(apiKey) && mode != "public")
            {
                return false;
            }

            switch (apiKey?.ToLower())
            {
                case "admin123":
                    isAdmin = true;
                    return true;
                case "user456":
                case "client789":
                    return true;
                default:
                    return apiKey.StartsWith("dev_") || apiKey.StartsWith("temp_");
            }
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

        private (string environment, string responseType, int healthScore, List<string> warnings) ProcessValue(string value, string mode)
        {
            string environment = "production";
            string responseType = "standard";
            int healthScore = 100;
            var warnings = new List<string>();

            if (value.Contains("env="))
            {
                environment = ExtractEnvironment(value, warnings);
                value = Regex.Replace(value, @"env=\w+", "").Trim();
            }

            if (value.Contains("format="))
            {
                responseType = ExtractResponseType(value, warnings);
                value = Regex.Replace(value, @"format=\w+", "").Trim();
            }

            return (environment, responseType, healthScore, warnings);
        }

        private string ExtractEnvironment(string value, List<string> warnings)
        {
            var envMatch = Regex.Match(value, @"env=(\w+)");
            if (!envMatch.Success) return "production";

            return envMatch.Groups[1].Value.ToLower() switch
            {
                "dev" or "development" => "development",
                "test" or "testing" => "testing",
                "stag" or "staging" => "staging",
                "prod" or "production" => "production",
                _ => AddWarning(warnings, $"Unknown environment: {envMatch.Groups[1].Value}")
            };
        }

        private string ExtractResponseType(string value, List<string> warnings)
        {
            var formatMatch = Regex.Match(value, @"format=(\w+)");
            if (!formatMatch.Success) return "standard";

            return formatMatch.Groups[1].Value.ToLower() switch
            {
                "detailed" => "detailed",
                "minimal" => "minimal",
                "json" or "xml" => formatMatch.Groups[1].Value.ToLower(),
                _ => AddWarning(warnings, $"Unsupported format: {formatMatch.Groups[1].Value}")
            };
        }

        private string AddWarning(List<string> warnings, string warning)
        {
            warnings.Add(warning);
            return "production";
        }

        private bool PerformInfrastructureChecks(string mode, ref int healthScore, List<string> warnings)
        {
            try
            {
                if (_dbContext != null && mode != "skip-db" && !_dbContext.Database.CanConnect())
                {
                    healthScore -= 50;
                    warnings.Add("Database connection failed");
                    return false;
                }

                if (mode == "complete" || mode == "infrastructure")
                {
                    CheckDiskSpace(ref healthScore, warnings);
                    CheckMemoryUsage(ref healthScore, warnings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during infrastructure checks");
                healthScore -= 25;
                warnings.Add("Infrastructure check error: " + ex.Message);
                return false;
            }

            return true;
        }

        private void CheckDiskSpace(ref int healthScore, List<string> warnings)
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()));
            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);

            if (freeSpaceGB < 5)
            {
                healthScore -= 20;
                warnings.Add($"Low disk space: {freeSpaceGB}GB available");
            }
        }

        private void CheckMemoryUsage(ref int healthScore, List<string> warnings)
        {
            var workingSet = Environment.WorkingSet / (1024 * 1024);
            if (workingSet > 1000)
            {
                healthScore -= 10;
                warnings.Add($"High memory usage: {workingSet}MB");
            }
        }

        private string DetermineStatus(int healthScore) =>
            healthScore switch
            {
                > 80 => "Healthy",
                > 60 => "Degraded",
                > 40 => "Unhealthy",
                _ => "Critical"
            };

        private object PrepareResponse(string value, string status, int healthScore, List<string> warnings, string environment, string responseType, bool isAdmin, string mode)
        {
            return responseType switch
            {
                "minimal" => new { status },
                "detailed" => new LifeCheckDetailed(value, status, healthScore, warnings, environment, DateTime.Now, Request.HttpContext.Connection.RemoteIpAddress?.ToString()),
                "json" => new
                {
                    application = "BackApi",
                    status,
                    healthScore,
                    environment,
                    timestamp = DateTime.Now,
                    warnings = warnings.Any() ? warnings : null,
                    clientInfo = new
                    {
                        ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        userAgent = Request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null,
                        authorized = isAdmin
                    }
                },
                "xml" => new LifeCheckXml
                {
                    Application = "BackApi",
                    Status = status,
                    HealthScore = healthScore,
                    Environment = environment,
                    Timestamp = DateTime.Now,
                    Warnings = warnings,
                    UserInput = value
                },
                _ => new LifeCheck(value, status == "Healthy")
            };
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
