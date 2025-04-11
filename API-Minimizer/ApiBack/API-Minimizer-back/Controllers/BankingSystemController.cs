using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankingSystem.Models;
using BankingSystem.Services;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace BankingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionProcessorController : ControllerBase
    {
        private readonly ITransactionService _txSvc;
        private readonly IAccountService _acctSvc;
        private readonly ILogger<TransactionProcessorController> _logger;
        private readonly BankingDbContext _ctx;
        private readonly IRiskAnalyzer _riskAnalyzer;

        public TransactionProcessorController(
            ITransactionService txSvc,
            IAccountService acctSvc,
            ILogger<TransactionProcessorController> logger,
            BankingDbContext ctx,
            IRiskAnalyzer riskAnalyzer)
        {
            _txSvc = txSvc ?? throw new ArgumentNullException(nameof(txSvc));
            _acctSvc = acctSvc ?? throw new ArgumentNullException(nameof(acctSvc));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _riskAnalyzer = riskAnalyzer ?? throw new ArgumentNullException(nameof(riskAnalyzer));
        }

        [HttpGet("analytics/summary")]
        [SwaggerOperation(Summary = "Obtiene un resumen analítico de las transacciones basado en criterios complejos")]
        public async Task<ActionResult<TxAnalyticsSummary>> GetTransactionAnalytics(
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate,
            [FromQuery] string groupBy = "category",
            [FromQuery] string aggregationType = "sum",
            [FromQuery] int timeGranularity = 30,
            [FromQuery] bool excludePending = true,
            [FromQuery] string metricFormat = "default")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Usuario no autenticado" });

            try {
                var accounts = await _ctx.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.Id)
                    .ToListAsync();
                
                if (!accounts.Any())
                    return BadRequest(new { message = "No se encontraron cuentas para este usuario" });

                var query = _ctx.Transactions
                    .Where(t => accounts.Contains(t.AccountId))
                    .Where(t => !excludePending || t.Status != TransactionStatus.Pending);
                
                if (startDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= startDate.Value);
                
                if (endDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= endDate.Value);

                // Compleja lógica de agrupación basada en parámetros dinámicos
                var metricsConfig = new Dictionary<string, Func<IQueryable<Transaction>, object>> {
                    ["category"] = q => q.GroupBy(t => t.CategoryId ?? Guid.Empty)
                        .Select(g => new {
                            CategoryId = g.Key,
                            CategoryName = g.Key != Guid.Empty ? 
                                _ctx.Categories.FirstOrDefault(c => c.Id == g.Key).Name : "Sin categoría",
                            TotalAmount = g.Sum(t => t.TransactionType == TransactionType.Debit ? -t.Amount : t.Amount),
                            Count = g.Count(),
                            AverageAmount = g.Average(t => t.Amount),
                            MinAmount = g.Min(t => t.Amount),
                            MaxAmount = g.Max(t => t.Amount),
                            FirstDate = g.Min(t => t.TransactionDate),
                            LastDate = g.Max(t => t.TransactionDate)
                        }).OrderByDescending(x => Math.Abs((decimal)x.TotalAmount)),
                    
                    ["time"] = q => {
                        var timeFormat = timeGranularity switch {
                            1 => "día",
                            7 => "semana",
                            30 => "mes",
                            90 => "trimestre",
                            365 => "año",
                            _ => "mes"
                        };
                        
                        return q.GroupBy(t => timeFormat switch {
                                "día" => t.TransactionDate.Date,
                                "semana" => t.TransactionDate.AddDays(-(int)t.TransactionDate.DayOfWeek).Date,
                                "mes" => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1),
                                "trimestre" => new DateTime(t.TransactionDate.Year, 
                                    ((t.TransactionDate.Month - 1) / 3) * 3 + 1, 1),
                                "año" => new DateTime(t.TransactionDate.Year, 1, 1),
                                _ => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1)
                            })
                            .Select(g => new {
                                PeriodStart = g.Key,
                                PeriodName = timeFormat == "día" ? g.Key.ToString("yyyy-MM-dd") :
                                             timeFormat == "semana" ? $"Semana del {g.Key:yyyy-MM-dd}" :
                                             timeFormat == "mes" ? g.Key.ToString("MMM yyyy") :
                                             timeFormat == "trimestre" ? $"T{((DateTime)g.Key).Month / 3 + 1} {((DateTime)g.Key).Year}" :
                                             $"Año {((DateTime)g.Key).Year}",
                                TotalIncome = g.Where(t => t.TransactionType == TransactionType.Credit)
                                               .Sum(t => t.Amount),
                                TotalExpense = g.Where(t => t.TransactionType == TransactionType.Debit)
                                                .Sum(t => t.Amount),
                                NetAmount = g.Sum(t => t.TransactionType == TransactionType.Credit ? 
                                                       t.Amount : -t.Amount),
                                Count = g.Count(),
                                TransactionIds = g.Select(t => t.Id).ToList()
                            }).OrderBy(x => x.PeriodStart);
                    },
                    
                    ["merchant"] = q => q.GroupBy(t => t.MerchantName ?? "Desconocido")
                        .Select(g => new {
                            MerchantName = g.Key,
                            TotalAmount = g.Sum(t => t.TransactionType == TransactionType.Debit ? -t.Amount : t.Amount),
                            Count = g.Count(),
                            AverageAmount = g.Average(t => t.Amount),
                            LastTransaction = g.Max(t => t.TransactionDate),
                            TransactionFrequency = (g.Max(t => t.TransactionDate) - g.Min(t => t.TransactionDate)).TotalDays / 
                                                   Math.Max(1, g.Count() - 1)
                        }).OrderByDescending(x => Math.Abs((decimal)x.TotalAmount)),
                };

                if (!metricsConfig.ContainsKey(groupBy.ToLower()))
                    return BadRequest(new { message = $"Criterio de agrupación no válido: {groupBy}" });

                var results = metricsConfig[groupBy.ToLower()](query);
                
                // Aplicar formato a métricas según parámetro metricFormat
                var formattedResults = ApplyMetricFormatting(results, metricFormat);

                // Calcular valores adicionales para enriquecer los datos
                var totalTxs = await query.CountAsync();
                var netBalance = await query.SumAsync(t => 
                    t.TransactionType == TransactionType.Credit ? t.Amount : -t.Amount);
                
                var avgTxSize = await query
                    .GroupBy(t => 1)
                    .Select(g => g.Average(t => t.Amount))
                    .FirstOrDefaultAsync();

                var dateRange = await query
                    .GroupBy(t => 1)
                    .Select(g => new { 
                        Start = g.Min(t => t.TransactionDate),
                        End = g.Max(t => t.TransactionDate)
                    })
                    .FirstOrDefaultAsync();

                // Análisis de riesgo sobre transacciones
                var riskMetrics = await _riskAnalyzer.CalculateUserRiskMetricsAsync(userId);
                
                return Ok(new TxAnalyticsSummary {
                    UserId = userId,
                    GroupBy = groupBy,
                    AggregationType = aggregationType,
                    TimeGranularity = timeGranularity,
                    Results = formattedResults,
                    TotalTransactions = totalTxs,
                    TotalNetAmount = netBalance,
                    AverageTransactionSize = avgTxSize,
                    DateRange = dateRange,
                    GeneratedAt = DateTime.UtcNow,
                    RiskMetrics = new RiskMetricsSummary {
                        OverallRiskScore = riskMetrics.OverallScore,
                        AnomalyIndex = riskMetrics.AnomalyIndex,
                        UnusualActivityLevel = riskMetrics.UnusualActivityLevel,
                        FlaggedTransactionsCount = riskMetrics.FlaggedTransactionsCount
                    }
                });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error obteniendo análisis para usuario {UserId}", userId);
                return StatusCode(500, new { 
                    message = "Error procesando análisis de transacciones", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Procesa múltiples transacciones en un lote con reglas de validación complejas")]
        [SwaggerResponse(201, "Lote de transacciones procesado correctamente")]
        [SwaggerResponse(400, "Datos de lote inválidos")]
        [SwaggerResponse(422, "Error de procesamiento del lote")]
        public async Task<ActionResult<BatchProcessResult>> ProcessTransactionBatch(
            [FromBody] TransactionBatchRequest batchRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Usuario no autenticado" });

            try {
                // Verificar autorización para todas las cuentas
                var accountIds = batchRequest.Transactions
                    .Select(t => t.AccountId)
                    .Distinct()
                    .ToList();
                
                var authorizedAccounts = await _ctx.Accounts
                    .Where(a => accountIds.Contains(a.Id) && a.UserId == userId)
                    .Select(a => a.Id)
                    .ToListAsync();
                
                var unauthorizedAccounts = accountIds.Except(authorizedAccounts).ToList();
                if (unauthorizedAccounts.Any())
                    return Forbid();
                
                // Validar cada transacción con reglas complejas
                var validationResults = new List<TransactionValidationResult>();
                var hasErrors = false;
                
                foreach (var tx in batchRequest.Transactions) {
                    // Realizar validaciones básicas
                    var errors = new List<string>();
                    
                    if (tx.Amount == 0)
                        errors.Add("El monto no puede ser cero");
                    
                    if (string.IsNullOrWhiteSpace(tx.Description))
                        errors.Add("La descripción es obligatoria");
                    
                    // Validar con expresiones regulares para detectar datos sensibles
                    var descriptionPattern = new Regex(@"(tarjeta|card|cuenta|account)\s*[#:]\s*\d{4}[-\s*]\d{4}[-\s*]\d{4}[-\s*]\d{4}",
                        RegexOptions.IgnoreCase);
                    
                    if (descriptionPattern.IsMatch(tx.Description))
                        errors.Add("La descripción contiene información financiera sensible");
                    
                    // Validar monto vs. balance disponible
                    var account = await _ctx.Accounts.FindAsync(tx.AccountId);
                    if (account != null && tx.Amount < 0 && Math.Abs(tx.Amount) > account.AvailableBalance)
                        errors.Add("Fondos insuficientes para realizar la transacción");
                    
                    // Análisis de riesgo para transacciones grandes
                    if (Math.Abs(tx.Amount) > 1000) {
                        var riskScore = await _riskAnalyzer.AnalyzeTransactionRiskAsync(tx, userId);
                        if (riskScore > 0.7m)
                            errors.Add($"Transacción de alto riesgo (Score: {riskScore:P0})");
                    }
                    
                    // Añadir resultado de validación
                    validationResults.Add(new TransactionValidationResult {
                        TransactionId = tx.Id ?? Guid.NewGuid(),
                        IsValid = !errors.Any(),
                        Errors = errors
                    });
                    
                    if (errors.Any())
                        hasErrors = true;
                }
                
                // Si alguna transacción falló, devolver los errores
                if (hasErrors || batchRequest.ValidateOnly) {
                    return batchRequest.ValidateOnly 
                        ? Ok(new BatchProcessResult {
                            Status = "Validated",
                            ProcessedTransactionsCount = 0,
                            TotalTransactionsCount = batchRequest.Transactions.Count,
                            ValidationResults = validationResults
                        })
                        : UnprocessableEntity(new BatchProcessResult {
                            Status = "Failed",
                            ProcessedTransactionsCount = 0,
                            TotalTransactionsCount = batchRequest.Transactions.Count,
                            ValidationResults = validationResults
                        });
                }
                
                // Procesar las transacciones con comportamiento inteligente
                var processedTxs = new List<ProcessedTransaction>();
                var strategies = new Dictionary<string, Func<TransactionRequest, Task<ProcessedTransaction>>> {
                    ["default"] = async tx => await _txSvc.ProcessStandardTransactionAsync(tx, userId),
                    ["recurring"] = async tx => await _txSvc.ProcessRecurringTransactionAsync(tx, userId),
                    ["split"] = async tx => await _txSvc.ProcessSplitTransactionAsync(tx, userId),
                    ["foreign"] = async tx => await _txSvc.ProcessForeignCurrencyTransactionAsync(tx, userId)
                };
                
                foreach (var tx in batchRequest.Transactions) {
                    var strategy = "default";
                    
                    // Determinar estrategia basada en propiedades
                    if (tx.IsRecurring)
                        strategy = "recurring";
                    else if (tx.SplitInfo != null && tx.SplitInfo.Categories?.Any() == true)
                        strategy = "split";
                    else if (!string.IsNullOrEmpty(tx.OriginalCurrency) && tx.OriginalCurrency != "USD")
                        strategy = "foreign";
                    
                    // Procesar la transacción con la estrategia correspondiente
                    var result = await strategies[strategy](tx);
                    processedTxs.Add(result);
                }
                
                // Aplicar reglas post-procesamiento para categorización automática
                await _txSvc.ApplyCategoryRulesAsync(processedTxs.Select(p => p.TransactionId).ToList(), userId);
                
                // Actualizar historial de balances para reporting
                foreach (var accountId in authorizedAccounts)
                    await _acctSvc.UpdateBalanceHistoryAsync(accountId);
                
                // Generar respuesta
                return Ok(new BatchProcessResult {
                    Status = "Completed",
                    ProcessedTransactionsCount = processedTxs.Count,
                    TotalTransactionsCount = batchRequest.Transactions.Count,
                    ProcessedTransactions = processedTxs,
                    BatchId = Guid.NewGuid(),
                    ProcessedDate = DateTime.UtcNow
                });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error procesando lote para usuario {UserId}", userId);
                return StatusCode(500, new { 
                    message = "Error procesando lote de transacciones", 
                    error = ex.Message 
                });
            }
        }
        
        [HttpGet("rules/patterns")]
        [SwaggerOperation(Summary = "Detecta patrones de transacciones automáticamente usando algoritmos ML")]
        public async Task<ActionResult<PatternDetectionResult>> DetectTransactionPatterns(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] decimal minConfidence = 0.6m,
            [FromQuery] int minOccurrences = 3,
            [FromQuery] bool includeInsights = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Usuario no autenticado" });

            try {
                // Obtener transacciones para análisis
                var query = _ctx.Transactions
                    .Include(t => t.Account)
                    .Include(t => t.Category)
                    .Where(t => t.Account.UserId == userId)
                    .Where(t => t.Status == TransactionStatus.Completed);
                
                if (startDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= startDate.Value);
                
                if (endDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= endDate.Value);
                
                var transactions = await query.ToListAsync();
                
                if (transactions.Count < minOccurrences * 2)
                    return Ok(new PatternDetectionResult {
                        PatternCount = 0,
                        Patterns = new List<TransactionPattern>(),
                        Message = "Datos insuficientes para detectar patrones"
                    });
                
                // Lógica compleja para detectar patrones
                var patterns = new List<TransactionPattern>();
                
                // 1. Detectar patrones de comerciantes recurrentes
                var merchantPatterns = transactions
                    .Where(t => !string.IsNullOrEmpty(t.MerchantName))
                    .GroupBy(t => t.MerchantName)
                    .Where(g => g.Count() >= minOccurrences)
                    .Select(g => {
                        // Calcular estadísticas para este comerciante
                        var txs = g.OrderBy(t => t.TransactionDate).ToList();
                        var amounts = txs.Select(t => t.TransactionType == TransactionType.Debit ? -t.Amount : t.Amount).ToList();
                        var intervals = new List<double>();
                        
                        for (int i = 1; i < txs.Count; i++)
                            intervals.Add((txs[i].TransactionDate - txs[i-1].TransactionDate).TotalDays);
                        
                        // Detectar si hay periodicidad
                        var avgInterval = intervals.Any() ? intervals.Average() : 0;
                        var stdDevInterval = intervals.Any() 
                            ? Math.Sqrt(intervals.Select(x => Math.Pow(x - avgInterval, 2)).Sum() / intervals.Count) 
                            : 0;
                        
                        var isRegular = intervals.Count >= 2 && stdDevInterval / avgInterval < 0.5;
                        var avgAmount = amounts.Average();
                        var stdDevAmount = Math.Sqrt(amounts.Select(x => Math.Pow(x - avgAmount, 2)).Sum() / amounts.Count);
                        var isConsistentAmount = stdDevAmount / Math.Abs(avgAmount) < 0.2;
                        
                        // Calcular fecha esperada de próxima transacción
                        DateTime? nextExpectedDate = isRegular 
                            ? txs.Last().TransactionDate.AddDays(avgInterval)
                            : null;
                            
                        var confidence = CalculatePatternConfidence(
                            isRegular, isConsistentAmount, intervals.Count, txs.Count);
                            
                        return new TransactionPattern {
                            PatternType = "Merchant",
                            PatternName = $"Transacciones en {g.Key}",
                            MerchantName = g.Key,
                            Occurrences = g.Count(),
                            AverageAmount = avgAmount,
                            AverageInterval = avgInterval,
                            IntervalUnit = "days",
                            LastOccurrence = txs.Last().TransactionDate,
                            NextExpectedDate = nextExpectedDate,
                            IsRegular = isRegular,
                            Confidence = confidence,
                            RelatedTransactions = txs.Select(t => t.Id).ToList(),
                            CategoryId = txs.GroupBy(t => t.CategoryId)
                                .OrderByDescending(g => g.Count())
                                .First().Key,
                            CategoryName = txs.GroupBy(t => t.CategoryId)
                                .OrderByDescending(g => g.Count())
                                .First().First().Category?.Name ?? "Sin categoría"
                        };
                    })
                    .Where(p => p.Confidence >= minConfidence)
                    .ToList();
                
                patterns.AddRange(merchantPatterns);
                
                // 2. Detectar patrones de categorías mensuales
                var monthlyCategoryPatterns = transactions
                    .GroupBy(t => new { 
                        Month = t.TransactionDate.Month,
                        Year = t.TransactionDate.Year,
                        CategoryId = t.CategoryId ?? Guid.Empty
                    })
                    .GroupBy(g => g.Key.CategoryId)
                    .Where(g => g.Count() >= minOccurrences)
                    .Select(categoryGroup => {
                        var categoryName = transactions
                            .FirstOrDefault(t => t.CategoryId == categoryGroup.Key)?.Category?.Name 
                            ?? "Sin categoría";
                            
                        var monthlyTotals = categoryGroup
                            .Select(g => new {
                                YearMonth = new DateTime(g.Key.Year, g.Key.Month, 1),
                                Total = g.Sum(t => t.TransactionType == TransactionType.Debit ? -t.Amount : t.Amount)
                            })
                            .OrderBy(x => x.YearMonth)
                            .ToList();
                            
                        var amounts = monthlyTotals.Select(m => m.Total).ToList();
                        var avgAmount = amounts.Average();
                        var stdDevAmount = Math.Sqrt(amounts.Select(x => Math.Pow(x - avgAmount, 2)).Sum() / amounts.Count);
                        var isConsistentAmount = stdDevAmount / Math.Abs(avgAmount) < 0.3;
                        
                        var monthsWithTxs = monthlyTotals.Select(m => m.YearMonth).ToList();
                        var allMonths = new List<DateTime>();
                        
                        if (monthsWithTxs.Any()) {
                            var startMonth = monthsWithTxs.Min();
                            var endMonth = monthsWithTxs.Max();
                            
                            for (var month = startMonth; month <= endMonth; month = month.AddMonths(1))
                                allMonths.Add(month);
                            
                            var coverageRatio = (double)monthsWithTxs.Count / allMonths.Count;
                            var confidence = isConsistentAmount ? (decimal)(0.7 * coverageRatio + 0.3) : (decimal)(0.5 * coverageRatio);
                            
                            if (confidence >= minConfidence) {
                                var relatedTxIds = transactions
                                    .Where(t => t.CategoryId == categoryGroup.Key)
                                    .Where(t => monthlyTotals.Any(m => 
                                        m.YearMonth.Year == t.TransactionDate.Year && 
                                        m.YearMonth.Month == t.TransactionDate.Month))
                                    .Select(t => t.Id)
                                    .ToList();
                                    
                                return new TransactionPattern {
                                    PatternType = "MonthlyCategorySpend",
                                    PatternName = $"Gasto mensual en {categoryName}",
                                    CategoryId = categoryGroup.Key,
                                    CategoryName = categoryName,
                                    Occurrences = monthlyTotals.Count,
                                    AverageAmount = avgAmount,
                                    AverageInterval = 30,
                                    IntervalUnit = "days",
                                    LastOccurrence = monthlyTotals.Last().YearMonth,
                                    NextExpectedDate = monthlyTotals.Last().YearMonth.AddMonths(1),
                                    IsRegular = coverageRatio > 0.7,
                                    Confidence = confidence,
                                    RelatedTransactions = relatedTxIds
                                };
                            }
                        }
                        
                        return null;
                    })
                    .Where(p => p != null)
                    .ToList();
                
                patterns.AddRange(monthlyCategoryPatterns);
                
                // Generar insights basados en los patrones
                var insights = new List<string>();
                if (includeInsights) {
                    // Insights para comerciantes recurrentes
                    foreach (var merchantPattern in patterns.Where(p => p.PatternType == "Merchant" && p.IsRegular)) {
                        insights.Add($"Detectamos pagos regulares de {FormatAmount(merchantPattern.AverageAmount)} " +
                            $"a {merchantPattern.MerchantName} aproximadamente cada " +
                            $"{FormatInterval(merchantPattern.AverageInterval)}");
                            
                        if (merchantPattern.NextExpectedDate.HasValue) {
                            insights.Add($"El próximo pago a {merchantPattern.MerchantName} se espera alrededor del " +
                                $"{merchantPattern.NextExpectedDate.Value:d MMMM}");
                        }
                    }
                    
                    // Insights para categorías mensuales
                    var monthlySpendPatterns = patterns
                        .Where(p => p.PatternType == "MonthlyCategorySpend" && p.AverageAmount < 0)
                        .OrderBy(p => p.AverageAmount)
                        .ToList();
                        
                    if (monthlySpendPatterns.Any()) {
                        insights.Add($"Sus principales gastos mensuales son:");
                        foreach (var pattern in monthlySpendPatterns.Take(3)) {
                            insights.Add($"- {pattern.CategoryName}: {FormatAmount(pattern.AverageAmount)} por mes");
                        }
                    }
                }
                
                return Ok(new PatternDetectionResult {
                    PatternCount = patterns.Count,
                    Patterns = patterns,
                    Insights = insights,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error detectando patrones para usuario {UserId}", userId);
                return StatusCode(500, new { 
                    message = "Error analizando patrones de transacciones", 
                    error = ex.Message 
                });
            }
        }

        // Métodos auxiliares privados
        private object ApplyMetricFormatting(object results, string format) {
            // Implementación de formateo para métricas
            return results;
        }
        
        private decimal CalculatePatternConfidence(
            bool isRegular, bool isConsistentAmount, int intervalCount, int totalOccurrences) {
            var baseConfidence = 0.5m;
            
            if (isRegular) baseConfidence += 0.2m;
            if (isConsistentAmount) baseConfidence += 0.2m;
            
            // Más intervalos = más confianza
            baseConfidence += Math.Min(0.1m, 0.02m * intervalCount);
            
            return Math.Min(1.0m, baseConfidence);
        }
        
        private string FormatAmount(decimal amount) {
            return Math.Abs(amount).ToString("C");
        }
        
        private string FormatInterval(double days) {
            if (days >= 365)
                return $"{days / 365:N1} años";
            if (days >= 30)
                return $"{days / 30:N1} meses";
            if (days >= 7)
                return $"{days / 7:N1} semanas";
            return $"{days:N1} días";
        }
    }

    // Clases auxiliares para DTOs (simplificadas para el ejemplo)
    public class TxAnalyticsSummary {
        public string UserId { get; set; }
        public string GroupBy { get; set; }
        public string AggregationType { get; set; }
        public int TimeGranularity { get; set; }
        public object Results { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public object DateRange { get; set; }
        public DateTime GeneratedAt { get; set; }
        public RiskMetricsSummary RiskMetrics { get; set; }
    }
    
    public class RiskMetricsSummary {
        public decimal OverallRiskScore { get; set; }
        public decimal AnomalyIndex { get; set; }
        public string UnusualActivityLevel { get; set; }
        public int FlaggedTransactionsCount { get; set; }
    }
    
    public class BatchProcessResult {
        public string Status { get; set; }
        public int ProcessedTransactionsCount { get; set; }
        public int TotalTransactionsCount { get; set; }
        public List<TransactionValidationResult> ValidationResults { get; set; }
        public List<ProcessedTransaction> ProcessedTransactions { get; set; }
        public Guid BatchId { get; set; }
        public DateTime ProcessedDate { get; set; }
    }
    
    public class TransactionValidationResult {
        public Guid TransactionId { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
    }
    
    public class ProcessedTransaction {
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string ProcessingDetails { get; set; }
    }
    
    public class TransactionPattern {
        public string PatternType { get; set; }
        public string PatternName { get; set; }
        public string MerchantName { get; set; }
        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Occurrences { get; set; }
        public decimal AverageAmount { get; set; }
        public double AverageInterval { get; set; }
        public string IntervalUnit { get; set; }
        public DateTime LastOccurrence { get; set; }
        public DateTime? NextExpectedDate { get; set; }
        public bool IsRegular { get; set; }
        public decimal Confidence { get; set; }
        public List<Guid> RelatedTransactions { get; set; }
    }
    
    public class PatternDetectionResult {
        public int PatternCount { get; set; }
        public List<TransactionPattern> Patterns { get; set; }
        public List<string> Insights { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Message { get; set; }
    }
    
    public class TransactionBatchRequest {
        public List<TransactionRequest> Transactions { get; set; }
        public bool ValidateOnly { get; set; }
        public string BatchReference { get; set; }
    }
    
    public class TransactionRequest {
        public Guid? Id { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Description { get; set; }
        public Guid? CategoryId { get; set; }
        public string MerchantName { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }
        public SplitTransactionInfo SplitInfo { get; set; }
        public string OriginalCurrency { get; set; }
        public decimal? OriginalAmount { get; set; }
    }
    
    public class SplitTransactionInfo {
        public List<CategorySplit> Categories { get; set; }
    }
    
    public class CategorySplit {
        public Guid CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}