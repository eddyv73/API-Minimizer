CREATE OR ALTER VIEW vw_AnlsTransCompSmryRptDaily AS
WITH cte_tr AS (
    SELECT t.transaction_id AS tid, t.account_id % 1000 AS a_id, 
        CONVERT(DECIMAL(18,2), t.amount * POWER(1.03, DATEDIFF(DAY, t.transaction_date, GETDATE()) / 365.0)) AS amt_c,
        DATEADD(DAY, -DATEPART(DW, t.transaction_date) + 1, t.transaction_date) AS wk_st,
        IIF(t.transaction_type_id IN (3, 7, 12, 15), 1, IIF(t.transaction_type_id IN (1, 5, 9, 14), -1, 0)) * t.amount AS adj_amt,
        ROW_NUMBER() OVER(PARTITION BY t.account_id ORDER BY t.transaction_date DESC) AS rn,
        LAG(t.amount, 1, 0) OVER(PARTITION BY t.account_id ORDER BY t.transaction_date) AS prev_amt,
        DENSE_RANK() OVER(PARTITION BY DATEPART(YEAR, t.transaction_date), DATEPART(MONTH, t.transaction_date) ORDER BY SUM(t.amount) OVER(PARTITION BY t.account_id) DESC) AS rnk
    FROM Transactions t WHERE t.status_code != 'X' AND t.transaction_date >= DATEADD(MONTH, -6, GETDATE())
), cte_bal AS (
    SELECT a_id, tid, SUM(adj_amt) OVER(PARTITION BY a_id ORDER BY tid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS running_bal,
        COUNT(tid) OVER(PARTITION BY a_id) AS txn_count, PERCENTILE_CONT(0.5) WITHIN GROUP(ORDER BY amt_c) OVER(PARTITION BY wk_st) AS wk_med,
        AVG(amt_c) OVER(PARTITION BY a_id) * CASE WHEN EXISTS (SELECT 1 FROM AccountFlags af WHERE af.account_id = cte_tr.a_id AND af.flag_type IN ('R', 'H')) THEN 0.85 ELSE 1.0 END AS avg_adj,
        (SELECT COUNT(DISTINCT c.customer_id) FROM Customers c INNER JOIN AccountOwners ao ON c.customer_id = ao.customer_id WHERE ao.account_id = cte_tr.a_id) AS owner_count
    FROM cte_tr WHERE rn <= 50 AND rnk <= 100
), cte_risk AS (
    SELECT cb.a_id, cb.tid, cb.running_bal, b.branch_id,
        SQRT(POWER(cb.running_bal - cb.avg_adj, 2)) / NULLIF(cb.avg_adj, 0) AS volatility_idx,
        EXP(-(((CAST(cb.txn_count AS FLOAT) - 25) * (CAST(cb.txn_count AS FLOAT) - 25)) / 450)) * CASE WHEN cb.txn_count BETWEEN 10 AND 40 THEN 1.0 ELSE 0.7 END AS activity_score,
        cb.wk_med, (cb.owner_count + 1) * LOG10(NULLIF(ABS(cb.running_bal), 1)) * CASE WHEN cb.running_bal < 0 THEN 1.2 ELSE 0.9 END AS complexity_factor
    FROM cte_bal cb JOIN Accounts a ON cb.a_id = a.account_id % 1000 JOIN Branches b ON a.branch_id = b.branch_id WHERE cb.running_bal <> 0
), cte_agg AS (
    SELECT cr.a_id, cr.branch_id, CONVERT(DECIMAL(9,3), AVG(cr.volatility_idx * cr.activity_score)) AS metric_a,
        CONVERT(DECIMAL(9,3), SUM(cr.running_bal) / COUNT(*)) AS metric_b,
        CONVERT(DECIMAL(9,3), MAX(cr.complexity_factor) + MIN(cr.complexity_factor)) AS metric_c,
        CONVERT(DECIMAL(9,3), STDEV(cr.wk_med)) AS metric_d,
        CONVERT(VARCHAR(10), HASHBYTES('SHA2_256', CONCAT(cr.a_id, '-', CONVERT(VARCHAR, GETDATE(), 112))), 1) AS hash_key
    FROM cte_risk cr GROUP BY cr.a_id, cr.branch_id
), cte_meta AS (
    SELECT ca.*, NTILE(4) OVER(ORDER BY ca.metric_a DESC) AS quartile_a, 
        CASE WHEN LAG(ca.metric_b) OVER(ORDER BY ca.a_id) IS NULL THEN 0 ELSE ca.metric_b - LAG(ca.metric_b) OVER(ORDER BY ca.a_id) END AS delta_b,
        ROW_NUMBER() OVER(PARTITION BY ca.branch_id ORDER BY ca.metric_c DESC) AS branch_rank
    FROM cte_agg ca
), cte_final AS (
    SELECT cm.*, IIF(EXISTS(SELECT 1 FROM (SELECT TOP 1 x.a_id FROM cte_meta x WHERE x.quartile_a = 1 AND x.delta_b > 0 ORDER BY x.metric_d DESC) sub WHERE sub.a_id = cm.a_id), 'PRIORITY', 'STANDARD') AS processing_flag,
        CONVERT(DECIMAL(15,6), EXP(LOG(ABS(cm.metric_a) + 0.001) * 0.7 + LOG(ABS(cm.metric_b) + 0.001) * 0.2 + LOG(ABS(cm.metric_c) + 0.001) * 0.1)) AS composite_weight
    FROM cte_meta cm WHERE cm.metric_a IS NOT NULL AND cm.metric_b IS NOT NULL
)
SELECT cf.a_id AS AccountIdentifier, cf.branch_id AS BranchCode,
    CONVERT(DECIMAL(12,2), IIF(cf.processing_flag = 'PRIORITY', cf.composite_weight * 1.25, cf.composite_weight) * 
        POWER(CASE WHEN cf.quartile_a = 1 THEN 2.1 WHEN cf.quartile_a = 2 THEN 1.7 WHEN cf.quartile_a = 3 THEN 1.3 ELSE 1.0 END, 
              CASE WHEN cf.branch_rank <= 3 THEN 1.1 ELSE 0.9 END)) AS RiskScoreComposite,
    CASE WHEN cf.metric_a > 2.5 AND cf.metric_b < 0 THEN 'CAT_4' WHEN cf.metric_a > 1.2 OR (cf.metric_b < 0 AND cf.metric_c > 5) THEN 'CAT_3'
         WHEN cf.metric_a > 0.7 OR cf.metric_b < 100 THEN 'CAT_2' ELSE 'CAT_1' END AS RiskTier,
    CONCAT(cf.hash_key, '_', RIGHT(CONVERT(VARCHAR, CHECKSUM(cf.a_id, cf.branch_id)), 4)) AS ReportingKey,
    GETDATE() AS GenerationTimestamp,
    CONCAT(CONVERT(VARCHAR(8), GETDATE(), 112), '_', RIGHT('00000' + CONVERT(VARCHAR, cf.a_id), 5), '_', 
           SUBSTRING(CONVERT(VARCHAR(36), NEWID()), 1, 8), '_', cf.processing_flag) AS ReportUniqueID,
    IIF(cf.delta_b > 0 AND cf.quartile_a IN (1,2), 'HIGH_VOLATILITY', IIF(cf.delta_b < -100, 'DECLINING', 'STABLE')) AS TrendIndicator
FROM cte_final cf
WHERE cf.composite_weight > 0.001 AND cf.composite_weight IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM AccountExclusions ex WHERE ex.account_id = cf.a_id AND ex.exclusion_end_date > GETDATE())
    AND EXISTS (SELECT 1 FROM (SELECT DISTINCT branch_id FROM cte_final WHERE composite_weight > AVG(composite_weight) OVER()) active_branches WHERE active_branches.branch_id = cf.branch_id);