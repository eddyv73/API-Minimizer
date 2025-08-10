using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinimizerCommon.Commons
{
    /// <summary>
    /// Service for handling health checks and infrastructure validation
    /// </summary>
    public interface IHealthService
    {
        Task<HealthCheckResult> PerformHealthCheckAsync(string mode);
        string DetermineStatus(int healthScore);
    }

    /// <summary>
    /// Service for API key validation
    /// </summary>
    public interface IApiKeyValidationService
    {
        bool ValidateApiKey(string? apiKey, string mode, out bool isAdmin);
    }

    /// <summary>
    /// Service for processing and formatting API responses
    /// </summary>
    public interface IResponseFormattingService
    {
        object PrepareResponse(string value, string status, int healthScore, 
            List<string> warnings, string environment, string responseType, 
            bool isAdmin, string mode, string? clientIp, string? userAgent);
    }

    /// <summary>
    /// Health check result model
    /// </summary>
    public class HealthCheckResult
    {
        public bool Success { get; set; }
        public int HealthScore { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Implementation of health service
    /// </summary>
    public class HealthService : IHealthService
    {
        private readonly IDbContext _dbContext;

        public HealthService(IDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<HealthCheckResult> PerformHealthCheckAsync(string mode)
        {
            var result = new HealthCheckResult { Success = true, HealthScore = 100 };

            try
            {
                // Database connectivity check
                if (_dbContext != null && mode != "skip-db" && !await _dbContext.CanConnectAsync())
                {
                    result.HealthScore -= 50;
                    result.Warnings.Add("Database connection failed");
                    result.Success = false;
                }

                // Extended checks for complete mode
                if (mode == "complete" || mode == "infrastructure")
                {
                    var diskCheck = CheckDiskSpace();
                    result.HealthScore -= diskCheck.penalty;
                    result.Warnings.AddRange(diskCheck.warnings);

                    var memoryCheck = CheckMemoryUsage();
                    result.HealthScore -= memoryCheck.penalty;
                    result.Warnings.AddRange(memoryCheck.warnings);
                }
            }
            catch (Exception ex)
            {
                result.HealthScore -= 25;
                result.Warnings.Add("Infrastructure check error: " + ex.Message);
                result.Success = false;
            }

            return result;
        }

        public string DetermineStatus(int healthScore) =>
            healthScore switch
            {
                > 80 => "Healthy",
                > 60 => "Degraded",
                > 40 => "Unhealthy",
                _ => "Critical"
            };

        private (int penalty, List<string> warnings) CheckDiskSpace()
        {
            var warnings = new List<string>();
            var driveInfo = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(System.IO.Directory.GetCurrentDirectory()) ?? "/");
            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);

            if (freeSpaceGB < 5)
            {
                warnings.Add($"Low disk space: {freeSpaceGB}GB available");
                return (20, warnings);
            }

            return (0, warnings);
        }

        private (int penalty, List<string> warnings) CheckMemoryUsage()
        {
            var warnings = new List<string>();
            var workingSet = Environment.WorkingSet / (1024 * 1024);
            if (workingSet > 1000)
            {
                warnings.Add($"High memory usage: {workingSet}MB");
                return (10, warnings);
            }

            return (0, warnings);
        }
    }

    /// <summary>
    /// Implementation of API key validation service
    /// </summary>
    public class ApiKeyValidationService : IApiKeyValidationService
    {
        public bool ValidateApiKey(string? apiKey, string mode, out bool isAdmin)
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
                    return apiKey?.StartsWith("dev_") == true || apiKey?.StartsWith("temp_") == true;
            }
        }
    }

    /// <summary>
    /// Implementation of response formatting service
    /// </summary>
    public class ResponseFormattingService : IResponseFormattingService
    {
        public object PrepareResponse(string value, string status, int healthScore,
            List<string> warnings, string environment, string responseType,
            bool isAdmin, string mode, string? clientIp, string? userAgent)
        {
            return responseType switch
            {
                "minimal" => new { status },
                "detailed" => new LifeCheckDetailed(value, status, healthScore, warnings, 
                    environment, DateTime.Now, clientIp),
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
                        ipAddress = clientIp,
                        userAgent,
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
    }
}