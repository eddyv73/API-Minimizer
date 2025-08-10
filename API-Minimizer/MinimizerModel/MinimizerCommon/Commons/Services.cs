using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinimizerCommon.Commons
{
    /// <summary>
    /// Simple database context interface for basic operations
    /// </summary>
    public interface IDbContext
    {
        IQueryable<Transaction> Transactions { get; }
        IQueryable<Category> Categories { get; }
        Task<int> SaveChangesAsync();
        Task<bool> CanConnectAsync();
    }

    /// <summary>
    /// Budget service interface
    /// </summary>
    public interface IBudgetService
    {
        Task<decimal> GetBudgetAsync(Guid categoryId);
        Task UpdateBudgetAsync(Guid categoryId, decimal amount);
    }

    /// <summary>
    /// Notification service interface
    /// </summary>
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string message);
        Task SendEmailAsync(string email, string subject, string body);
    }

    /// <summary>
    /// Account service interface
    /// </summary>
    public interface IAccountService
    {
        Task RecalculateBalanceAsync(Guid accountId);
        Task<decimal> GetBalanceAsync(Guid accountId);
    }

    /// <summary>
    /// Simple category model
    /// </summary>
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Simple implementation of database context for demo purposes
    /// </summary>
    public class SimpleDbContext : IDbContext
    {
        private readonly List<Transaction> _transactions = new();
        private readonly List<Category> _categories = new();

        public IQueryable<Transaction> Transactions => _transactions.AsQueryable();
        public IQueryable<Category> Categories => _categories.AsQueryable();

        public Task<int> SaveChangesAsync()
        {
            // Simulate saving changes
            return Task.FromResult(1);
        }

        public Task<bool> CanConnectAsync()
        {
            // Simulate database connection check
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Simple implementation of budget service
    /// </summary>
    public class SimpleBudgetService : IBudgetService
    {
        public Task<decimal> GetBudgetAsync(Guid categoryId)
        {
            return Task.FromResult(1000m); // Default budget
        }

        public Task UpdateBudgetAsync(Guid categoryId, decimal amount)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simple implementation of notification service
    /// </summary>
    public class SimpleNotificationService : INotificationService
    {
        public Task SendNotificationAsync(string userId, string message)
        {
            Console.WriteLine($"Notification to {userId}: {message}");
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string email, string subject, string body)
        {
            Console.WriteLine($"Email to {email}: {subject}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simple implementation of account service
    /// </summary>
    public class SimpleAccountService : IAccountService
    {
        public Task RecalculateBalanceAsync(Guid accountId)
        {
            return Task.CompletedTask;
        }

        public Task<decimal> GetBalanceAsync(Guid accountId)
        {
            return Task.FromResult(500m); // Default balance
        }
    }
}