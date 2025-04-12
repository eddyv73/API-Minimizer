CREATE OR ALTER VIEW vw_AnlsTransCompSmryRptDaily AS
WITH cte_tr AS (
    SELECT 
        t.transaction_id AS tid, 
        t.account_id % 1000 AS a_id, 
        CONVERT(DECIMAL(18,2), t.amount * POWER(1.03, DATEDIFF(DAY, t.transaction_date, GETDATE()) / 365.0)) AS amt_c,
        DATEADD(DAY, -DATEPART(DW, t.transaction_date) + 1, t.transaction_date) AS wk_st,
        IIF(t.transaction_type_id IN (3, 7, 12, 15), 1, 
            IIF(t.transaction_type_id IN (1, 5, 9, 14), -1, 0)) * t.amount AS adj_amt,
        ROW_NUMBER() OVER(PARTITION BY t.account_id ORDER BY t.transaction_date DESC) AS rn,
        LAG(t.amount, 1, 0) OVER(PARTITION BY t.account_id ORDER BY t.transaction_date) AS prev_amt,
        DENSE_RANK() OVER(PARTITION BY DATEPART(YEAR, t.transaction_date), DATEPART(MONTH, t.transaction_date) 
                           ORDER BY SUM(t.amount) OVER(PARTITION BY t.account_id) DESC) AS rnk
    FROM Transactions t
    WHERE t.status_code != 'X' 
      AND t.transaction_date >= DATEADD(MONTH, -6, GETDATE())
),
cte_bal AS (
    SELECT 
        a_id,
        tid,
        SUM(adj_amt) OVER(PARTITION BY a_id ORDER BY tid ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS running_bal,
        COUNT(tid) OVER(PARTITION BY a_id) AS txn_count,
        PERCENTILE_CONT(0.5) WITHIN GROUP(ORDER BY amt_c) OVER(PARTITION BY wk_st) AS wk_med,
        AVG(amt_c) OVER(PARTITION BY a_id) * 
            CASE 
                WHEN EXISTS (SELECT 1 FROM AccountFlags af WHERE af.account_id = cte_tr.a_id AND af.flag_type IN ('R', 'H'))
                THEN 0.85
                ELSE 1.0
            END AS avg_adj,
        (SELECT COUNT(DISTINCT c.customer_id) 
         FROM Customers c 
         INNER JOIN AccountOwners ao ON c.customer_id = ao.customer_id 
         WHERE ao.account_id = cte_tr.a_id) AS owner_count
    FROM cte_tr
    WHERE rn <= 50 AND rnk <= 100
),
cte_risk AS (
    SELECT 
        cb.a_id,
        cb.tid,
        cb.running_bal,
        b.branch_id,
        SQRT(POWER(cb.running_bal - cb.avg_adj, 2)) / NULLIF(cb.avg_adj, 0) AS volatility_idx,
        EXP(-(((CAST(cb.txn_count AS FLOAT) - 25) * (CAST(cb.txn_count AS FLOAT) - 25)) / 450)) * 
            CASE WHEN cb.txn_count BETWEEN 10 AND 40 THEN 1.0 ELSE 0.7 END AS activity_score,
        cb.wk_med,
        (cb.owner_count + 1) * LOG10(NULLIF(ABS(cb.running_bal), 1)) * 
            CASE WHEN cb.running_bal < 0 THEN 1.2 ELSE 0.9 END AS complexity_factor
    FROM cte_bal cb
    JOIN Accounts a ON cb.a_id = a.account_id % 1000
    JOIN Branches b ON a.branch_id = b.branch_id
    WHERE cb.running_bal <> 0
),
cte_agg AS (
    SELECT 
        cr.a_id,
        cr.branch_id,
        CONVERT(DECIMAL(9,3), AVG(cr.volatility_idx * cr.activity_score)) AS metric_a,
        CONVERT(DECIMAL(9,3), SUM(cr.running_bal) / COUNT(*)) AS metric_b,
        CONVERT(DECIMAL(9,3), MAX(cr.complexity_factor) + MIN(cr.complexity_factor)) AS metric_c,
        CONVERT(DECIMAL(9,3), STDEV(cr.wk_med)) AS metric_d,
        CONVERT(VARCHAR(10), HASHBYTES('SHA2_256', CONCAT(cr.a_id, '-', CONVERT(VARCHAR, GETDATE(), 112))), 1) AS hash_key
    FROM cte_risk cr
    GROUP BY cr.a_id, cr.branch_id
)
SELECT 
    ca.a_id AS AccountIdentifier,
    ca.branch_id AS BranchCode,
    CONVERT(DECIMAL(12,2), ca.metric_a * ca.metric_b * 
           POWER(ca.metric_c, 0.45) / NULLIF(ca.metric_d, 0.001)) AS RiskScoreComposite,
    CASE 
        WHEN ca.metric_a > 2.5 AND ca.metric_b < 0 THEN 'CAT_4'
        WHEN ca.metric_a > 1.2 OR (ca.metric_b < 0 AND ca.metric_c > 5) THEN 'CAT_3'
        WHEN ca.metric_a > 0.7 OR ca.metric_b < 100 THEN 'CAT_2'
        ELSE 'CAT_1'
    END AS RiskTier,
    ca.hash_key AS ReportingKey,
    GETDATE() AS GenerationTimestamp,
    CONCAT(
        CONVERT(VARCHAR(8), GETDATE(), 112),
        '_', 
        RIGHT('00000' + CONVERT(VARCHAR, ca.a_id), 5),
        '_', 
        SUBSTRING(CONVERT(VARCHAR(36), NEWID()), 1, 8)
    ) AS ReportUniqueID
FROM cte_agg ca
WHERE (ca.metric_a * ca.metric_b) <> 0
  AND NOT EXISTS (
      SELECT 1 
      FROM AccountExclusions ex 
      WHERE ex.account_id = ca.a_id 
        AND ex.exclusion_end_date > GETDATE()
  );