using System;
using System.Collections.Generic;

namespace MinimizerCommon.Commons
{
    /// <summary>
    /// Detailed life check response model
    /// </summary>
    public class LifeCheckDetailed
    {
        public LifeCheckDetailed(string value, string status, int healthScore, List<string> warnings, 
            string environment, DateTime timestamp, string? clientIp)
        {
            Value = value;
            Status = status;
            HealthScore = healthScore;
            Warnings = warnings ?? new List<string>();
            Environment = environment;
            Timestamp = timestamp;
            ClientIp = clientIp;
        }

        public string Value { get; set; }
        public string Status { get; set; }
        public int HealthScore { get; set; }
        public List<string> Warnings { get; set; }
        public string Environment { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ClientIp { get; set; }
    }

    /// <summary>
    /// XML formatted life check response
    /// </summary>
    public class LifeCheckXml
    {
        public string Application { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int HealthScore { get; set; }
        public string Environment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string UserInput { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation error response with detailed errors
    /// </summary>
    public class ValidationErrorResponse : ErrorResponse
    {
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Transaction request model
    /// </summary>
    public class TransactionRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public Guid? CategoryId { get; set; }
        public bool IsRecurring { get; set; }
        public List<string> Tags { get; set; } = new();
        public TransactionType Type { get; set; }
    }

    /// <summary>
    /// Transaction response model
    /// </summary>
    public class TransactionResponse
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Transaction entity model
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public Guid? CategoryId { get; set; }
        public bool IsRecurring { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid AccountId { get; set; }
    }

    /// <summary>
    /// Transaction type enumeration
    /// </summary>
    public enum TransactionType
    {
        Income = 1,
        Expense = 2
    }
}