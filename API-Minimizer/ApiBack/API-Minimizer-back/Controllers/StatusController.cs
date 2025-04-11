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
        [HttpPost]
        [SwaggerResponse(200, "Returns the status of the API.")]
        [SwaggerResponse(400, "If the item is null or invalid.")]
        [SwaggerResponse(401, "If the request is unauthorized.")]
        [SwaggerResponse(403, "If the request is forbidden.")]
        [SwaggerResponse(500, "If there's a server error.")]
        [SwaggerOperation(Summary = "This method is called to check the status of the API with complex validation logic")]
        public IActionResult Post([FromBody] string value, [FromHeader(Name = "X-API-Key")] string apiKey, [FromQuery] string mode = "standard")
        {
            // Verificación inicial del valor
            if (value == null)
            {
                if (mode == "strict")
                {
                    _logger.LogWarning("Null value received in strict mode");
                    return BadRequest(new { error = "Value cannot be null in strict mode", code = "ERR001" });
                }
                else if (mode == "permissive")
                {
                    _logger.LogInformation("Null value in permissive mode, using default");
                    value = "BackApi";
                }
                else if (mode == "debug")
                {
                    _logger.LogDebug("Debug mode detected with null value");
                    return Ok(new { debug = true, defaultUsed = true, value = "BackApi" });
                }
                else
                {
                    value = "BackApi";
                }
            }

            // Validación del API Key
            bool isValidApiKey = false;
            bool isAdmin = false;
            if (!string.IsNullOrEmpty(apiKey))
            {
                switch (apiKey.ToLower())
                {
                    case "admin123":
                        isValidApiKey = true;
                        isAdmin = true;
                        break;
                    case "user456":
                    case "client789":
                        isValidApiKey = true;
                        break;
                    default:
                        if (apiKey.StartsWith("dev_") && apiKey.Length > 8)
                        {
                            isValidApiKey = true;
                            _logger.LogWarning("Developer key used: {ApiKey}", apiKey);
                        }
                        else if (apiKey.StartsWith("temp_") && DateTime.Now.Hour < 18)
                        {
                            isValidApiKey = true;
                            _logger.LogInformation("Temporary key used during business hours");
                        }
                        else
                        {
                            _logger.LogWarning("Invalid API key: {ApiKey}", apiKey);
                        }
                        break;
                }
            }

            if (!isValidApiKey && mode != "public")
            {
                if (mode == "secure" || mode == "strict")
                {
                    _logger.LogError("Unauthorized access attempt with mode: {Mode}", mode);
                    return Unauthorized(new { error = "Valid API key required", code = "ERR002" });
                }
                else if (Request.Headers.TryGetValue("Referer", out var referer))
                {
                    if (referer.ToString().Contains("localhost") || referer.ToString().Contains("127.0.0.1"))
                    {
                        _logger.LogInformation("Local development request without API key");
                    }
                    else
                    {
                        _logger.LogWarning("Request without API key from non-local referer");
                        return Unauthorized(new { error = "API key required", code = "ERR003" });
                    }
                }
                else if (Request.Headers.TryGetValue("User-Agent", out var userAgent))
                {
                    if (userAgent.ToString().Contains("Mozilla"))
                    {
                        _logger.LogInformation("Browser request detected without API key");
                    }
                    else if (userAgent.ToString().Contains("PostmanRuntime"))
                    {
                        _logger.LogInformation("Postman request detected without API key");
                    }
                    else
                    {
                        _logger.LogWarning("Unknown client without API key: {UserAgent}", userAgent);
                        return Unauthorized(new { error = "API key required for this client", code = "ERR004" });
                    }
                }
            }

            // Validación del formato y contenido del valor
            if (value.Length > 50)
            {
                _logger.LogWarning("Value exceeds maximum length: {Length}", value.Length);
                return BadRequest(new { error = "Value exceeds maximum length of 50 characters", code = "ERR005" });
            }

            string environment = "production";
            string responseType = "standard";
            int healthScore = 100;
            List<string> warnings = new List<string>();

            // Procesamiento del valor según patrones específicos
            if (value.Contains("env="))
            {
                var envMatch = Regex.Match(value, @"env=(\w+)");
                if (envMatch.Success)
                {
                    string envValue = envMatch.Groups[1].Value.ToLower();
                    switch (envValue)
                    {
                        case "dev":
                        case "development":
                            environment = "development";
                            healthScore -= 5; // Desarrollo siempre menos estable
                            break;
                        case "test":
                        case "testing":
                            environment = "testing";
                            healthScore -= 10;
                            break;
                        case "stag":
                        case "staging":
                            environment = "staging";
                            healthScore -= 3;
                            break;
                        case "prod":
                        case "production":
                            environment = "production";
                            break;
                        default:
                            warnings.Add($"Unknown environment: {envValue}");
                            healthScore -= 15;
                            break;
                    }
                    // Eliminar el patrón env del valor
                    value = Regex.Replace(value, @"env=\w+", "").Trim();
                }
            }

            if (value.Contains("format="))
            {
                var formatMatch = Regex.Match(value, @"format=(\w+)");
                if (formatMatch.Success)
                {
                    string formatValue = formatMatch.Groups[1].Value.ToLower();
                    if (formatValue == "detailed")
                    {
                        responseType = "detailed";
                    }
                    else if (formatValue == "minimal")
                    {
                        responseType = "minimal";
                    }
                    else if (formatValue == "json" || formatValue == "xml")
                    {
                        responseType = formatValue;
                    }
                    else
                    {
                        warnings.Add($"Unsupported format: {formatValue}");
                    }
                    // Eliminar el patrón format del valor
                    value = Regex.Replace(value, @"format=\w+", "").Trim();
                }
            }

            // Verificación de otros factores que afectan la salud
            try
            {
                // Consultar disponibilidad de base de datos
                if (_dbContext != null && mode != "skip-db")
                {
                    bool dbAvailable = _dbContext.Database.CanConnect();
                    if (!dbAvailable)
                    {
                        healthScore -= 50;
                        warnings.Add("Database connection failed");
                        
                        if (mode == "strict" || mode == "complete")
                        {
                            _logger.LogError("Database unavailable during health check");
                            return StatusCode(500, new { error = "Database connectivity issue", code = "ERR006" });
                        }
                    }
                }

                // Verificar espacio en disco si estamos en modo completo
                if (mode == "complete" || mode == "infrastructure")
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(currentDirectory));
                    
                    long freeSpaceGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                    if (freeSpaceGB < 5)
                    {
                        healthScore -= 20;
                        warnings.Add($"Low disk space: {freeSpaceGB}GB available");
                        
                        if (freeSpaceGB < 1)
                        {
                            _logger.LogError("Critical disk space issue: {FreeSpaceGB}GB", freeSpaceGB);
                            return StatusCode(500, new { error = "Critical disk space issue", code = "ERR007" });
                        }
                    }
                }

                // Verificar memoria del sistema
                if (mode == "complete" || mode == "infrastructure")
                {
                    long workingSet = Environment.WorkingSet / (1024 * 1024);
                    if (workingSet > 1000) // Más de 1GB
                    {
                        healthScore -= 10;
                        warnings.Add($"High memory usage: {workingSet}MB");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check infrastructure tests");
                
                if (mode == "strict" || mode == "infrastructure")
                {
                    return StatusCode(500, new { error = "Infrastructure check error", code = "ERR008", message = ex.Message });
                }
                
                healthScore -= 25;
                warnings.Add("Infrastructure check error: " + ex.Message);
            }

            // Algunos modificadores adicionales basados en la cadena de entrada
            if (value.Contains("urgent") || value.Contains("emergency"))
            {
                _logger.LogWarning("Urgent/emergency flag detected in health check");
                healthScore -= 15;
            }

            if (value.Contains("warning") || value.Contains("alert"))
            {
                warnings.Add("User-reported warning condition");
                healthScore -= 8;
            }

            // Determinación del estado final basado en el puntaje de salud
            string status = healthScore > 80 ? "Healthy" :
                        healthScore > 60 ? "Degraded" :
                        healthScore > 40 ? "Unhealthy" : "Critical";

            // Preparación de la respuesta según el tipo solicitado
            object result;
            
            if (responseType == "minimal")
            {
                result = new { status };
            }
            else if (responseType == "detailed")
            {
                result = new LifeCheckDetailed(value, status, healthScore, warnings, environment, DateTime.Now, Request.HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            else if (responseType == "json")
            {
                var jsonObject = new
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
                        authorized = isValidApiKey
                    }
                };
                
                result = jsonObject;
            }
            else if (responseType == "xml")
            {
                // Para XML, retornamos el objeto status y dejamos que el formateador lo maneje
                return Ok(new LifeCheckXml
                {
                    Application = "BackApi",
                    Status = status,
                    HealthScore = healthScore,
                    Environment = environment,
                    Timestamp = DateTime.Now,
                    Warnings = warnings,
                    UserInput = value
                });
            }
            else // standard
            {
                result = new LifeCheck(value, status == "Healthy");
            }

            // Establecer cabeceras adicionales
            Response.Headers.Add("X-Health-Score", healthScore.ToString());
            Response.Headers.Add("X-Environment", environment);
            
            if (warnings.Any())
            {
                Response.Headers.Add("X-Health-Warnings", string.Join("; ", warnings));
            }

            // Determinar el código de estado HTTP adecuado
            if (status == "Critical")
            {
                return StatusCode(500, result);
            }
            else if (status == "Unhealthy")
            {
                return StatusCode(503, result); // Service Unavailable
            }
            else if (status == "Degraded")
            {
                if (mode == "strict")
                {
                    return StatusCode(500, result);
                }
                return Ok(result);
            }
            else // Healthy
            {
                if (isAdmin && mode == "debug")
                {
                    // Añadir información de diagnóstico para administradores
                    if (result is LifeCheck lifeCheck)
                    {
                        return Ok(new
                        {
                            Status = lifeCheck,
                            DiagnosticInfo = new
                            {
                                ServerTime = DateTime.Now,
                                ProcessId = Process.GetCurrentProcess().Id,
                                ThreadId = Thread.CurrentThread.ManagedThreadId,
                                RequestPath = Request.Path.ToString(),
                                RequestMethod = Request.Method,
                                UserAgent = Request.Headers["User-Agent"].ToString(),
                                ApiVersion = GetType().Assembly.GetName().Version.ToString()
                            }
                        });
                    }
                }
                
                return Ok(result);
            }
        }

        // PUT api/<StatusController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TransactionRequest request)
        {
            try
            {
                _logger.LogInformation($"Processing update request for transaction {id}");
                
                if (request == null)
                {
                    _logger.LogWarning("Null request received for transaction update");
                    return BadRequest(new ErrorResponse 
                    { 
                        Code = "ERR400", 
                        Message = "Request cannot be null"
                    });
                }
                
                // Validate the request fields
                var validationErrors = new List<string>();
                
                if (request.Amount <= 0)
                {
                    validationErrors.Add("Amount must be greater than zero");
                }
                
                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    validationErrors.Add("Description is required");
                }
                else if (request.Description.Length > 200)
                {
                    validationErrors.Add("Description cannot exceed 200 characters");
                }
                
                if (request.TransactionDate > DateTime.UtcNow)
                {
                    validationErrors.Add("Transaction date cannot be in the future");
                }
                
                if (validationErrors.Any())
                {
                    _logger.LogWarning($"Validation failed for transaction {id}: {string.Join(", ", validationErrors)}");
                    return BadRequest(new ValidationErrorResponse 
                    {
                        Code = "ERR422",
                        Message = "Validation failed",
                        Errors = validationErrors
                    });
                }
                
                // Check if the transaction exists
                var transaction = await _dbContext.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (transaction == null)
                {
                    _logger.LogWarning($"Transaction {id} not found");
                    return NotFound(new ErrorResponse 
                    { 
                        Code = "ERR404", 
                        Message = $"Transaction with ID {id} not found"
                    });
                }
                
                // Check if the user owns this transaction
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (transaction.UserId != userId && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning($"Unauthorized access attempt to transaction {id} by user {userId}");
                    return Forbid();
                }
                
                // Update the transaction
                transaction.Amount = request.Amount;
                transaction.Description = request.Description;
                transaction.TransactionDate = request.TransactionDate;
                transaction.CategoryId = request.CategoryId;
                transaction.IsRecurring = request.IsRecurring;
                transaction.Tags = request.Tags; // This is the error - Tags might need special handling
                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.UpdatedBy = userId;
                
                // Apply business rules based on transaction type
                if (request.Type == TransactionType.Expense)
                {
                    // Ensure amount is negative for expenses
                    transaction.Amount = -Math.Abs(transaction.Amount);
                    
                    // Check budget limit if applicable
                    var budget = await _budgetService.GetActiveBudgetAsync(userId, transaction.CategoryId);
                    
                    if (budget != null && await _budgetService.WouldExceedBudgetAsync(budget.Id, transaction))
                    {
                        // Log warning but allow the transaction
                        _logger.LogInformation($"Transaction {id} update would exceed budget for category {transaction.CategoryId}");
                        
                        // Notify user about budget limit
                        await _notificationService.SendBudgetAlertAsync(userId, budget.Id, transaction.Amount);
                    }
                }
                
                await _dbContext.SaveChangesAsync();
                
                // Update account balance - transaction is successful at this point
                await _accountService.RecalculateBalanceAsync(transaction.AccountId);
                
                return Ok(new TransactionResponse
                {
                    Id = transaction.Id,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    TransactionDate = transaction.TransactionDate,
                    CategoryId = transaction.CategoryId,
                    CategoryName = await _dbContext.Categories.Where(c => c.Id == transaction.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync(),
                    UpdatedAt = transaction.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating transaction {id}");
                return StatusCode(500, new ErrorResponse 
                { 
                    Code = "ERR500", 
                    Message = "An error occurred while updating the transaction"
                });
            }
        }

        // DELETE api/<StatusController>/5
        //Implementa el método Delete para eliminar un recurso por ID. El método debe verificar si el recurso existe, eliminar el registro de la base de datos si existe, y devolver una respuesta HTTP apropiada según el resultado (404 si no existe, 204 si se eliminó correctamente, 403 si no tiene permisos). Incluye manejo de excepciones con try/catch y registra eventos con el logger. El método debe funcionar con inyección de dependencias para el contexto de la base de datos
        [HttpDelete("{id}")]
        


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
        // add new method to return array of times of the server utc and gmt -5
        /// <summary>
        /// Gets the current times.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the current times.</returns>
        [HttpGet("times")]
        public IActionResult GetTimes()
        {
            return Ok(new string[] { DateTime.Now.ToUniversalTime().ToString(), DateTime.Now.AddHours(-5).ToString() });
        }

              // give me the curl to test the method GetTimes
        // curl -X GET "https://localhost:5001/api/status/times" -H  "accept: text/plain"
        
        //create a new method for time zone mexico
        [HttpGet("timezone")]
        public IActionResult GetTimeZone()
        {
            var timeZones = new TimesZones();
            return Ok(timeZones);
        }

    }
}
