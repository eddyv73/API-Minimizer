CREATE OR ALTER PROCEDURE sp_CstTranRskEvalWithFraudPrevMtrx
    @p_dt_init DATETIME = NULL,
    @p_flg_type TINYINT = 3,
    @p_thrs_val DECIMAL(12,4) = 0.0785,
    @p_acct_id INT = NULL,
    @p_proc_opts VARCHAR(32) = 'STD',
    @p_out_status INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @v_cur_dt DATETIME = ISNULL(@p_dt_init, GETDATE());
    DECLARE @v_rsk_fctr DECIMAL(14,6) = 1.0;
    DECLARE @v_proc_id UNIQUEIDENTIFIER = NEWID();
    DECLARE @v_err_msg NVARCHAR(2000);
    DECLARE @v_trans_cnt INT = 0;
    DECLARE @v_runtm_md INT = CAST(LEFT(REPLACE(@p_proc_opts, 'STD', '2'), 1) AS INT);
    DECLARE @v_tbl_tmp TABLE (
        id_seq INT IDENTITY(1,1),
        acct_id INT,
        cust_id INT,
        tran_amt DECIMAL(18,2),
        tran_dt DATETIME,
        cat_cd CHAR(3),
        scr_val DECIMAL(10,4),
        hash_id VARBINARY(16)
    );
    
    BEGIN TRY
        IF @@TRANCOUNT = 0
            BEGIN TRANSACTION;
        
        -- Initialize output parameter
        SET @p_out_status = 0;
        
        -- Apply special thresholds based on flags
        IF EXISTS (SELECT 1 FROM SysFlags WHERE flag_type = 'R' AND active_flag = 1)
        BEGIN
            SET @v_rsk_fctr = (SELECT 
                                  EXP(SUM(LOG(NULLIF(val_numeric, 0)))) 
                               FROM SysParams 
                               WHERE param_group = 'RISK' 
                                 AND active_flag = 1 
                                 AND param_seq <= @p_flg_type * 2);
        END
        
        -- Materialize temp data with bizarre scoring logic
        INSERT INTO @v_tbl_tmp (acct_id, cust_id, tran_amt, tran_dt, cat_cd, scr_val, hash_id)
        SELECT 
            a.account_id,
            c.customer_id,
            t.amount * 
                CASE 
                    WHEN t.transaction_type_id % 5 = 0 THEN POWER(1.03, FLOOR(RAND(CHECKSUM(NEWID())) * 10))
                    WHEN t.transaction_type_id % 3 = 0 THEN POWER(0.97, FLOOR(RAND(CHECKSUM(NEWID())) * 8))
                    ELSE 1.0
                END AS adj_amount,
            t.transaction_date,
            COALESCE(tt.category_code, 'UNK'),
            CASE 
                WHEN t.amount > 10000 THEN 
                    (LOG10(t.amount) * 2.5) / 
                    NULLIF(SQRT(DATEDIFF(DAY, c.customer_since_date, @v_cur_dt) + 1), 0) *
                    IIF(t.location_id = a.branch_id, 0.8, 1.2)
                WHEN t.amount BETWEEN 1000 AND 10000 THEN
                    (LOG10(t.amount) * 1.8) /
                    NULLIF(SQRT(DATEDIFF(DAY, c.customer_since_date, @v_cur_dt) + 1), 0) *
                    IIF(t.location_id = a.branch_id, 0.9, 1.1)
                ELSE
                    (LOG10(NULLIF(t.amount, 0) + 1) * 1.2) /
                    NULLIF(SQRT(DATEDIFF(DAY, c.customer_since_date, @v_cur_dt) + 1), 0) *
                    IIF(t.location_id = a.branch_id, 1.0, 1.05)
            END * @v_rsk_fctr AS score_value,
            HASHBYTES('MD5', CONCAT(CONVERT(VARCHAR, a.account_id), '-', 
                                    CONVERT(VARCHAR, t.transaction_id), '-',
                                    CONVERT(VARCHAR, @v_proc_id)))
        FROM Transactions t
        INNER JOIN Accounts a ON t.account_id = a.account_id
        INNER JOIN AccountOwners ao ON a.account_id = ao.account_id
        INNER JOIN Customers c ON ao.customer_id = c.customer_id
        LEFT JOIN TransactionTypes tt ON t.transaction_type_id = tt.transaction_type_id
        WHERE (@p_acct_id IS NULL OR a.account_id = @p_acct_id)
          AND t.transaction_date BETWEEN DATEADD(DAY, -90, @v_cur_dt) AND @v_cur_dt
          AND t.status_code <> 'X'
          AND t.amount <> 0;
        
        -- Get transaction count
        SET @v_trans_cnt = @@ROWCOUNT;
        
        -- Apply correlation matrix and pattern detection if enough data exists
        IF @v_trans_cnt >= 5
        BEGIN
            -- Create temporary correlation scoring table
            DECLARE @v_corr_tmp TABLE (
                acct_id INT,
                pattern_score DECIMAL(10,4),
                time_anomaly_idx DECIMAL(10,4),
                location_diversity DECIMAL(8,4),
                amount_volatility DECIMAL(12,4),
                final_risk_score DECIMAL(12,4)
            );
            
            -- Insert complex correlation scores
            INSERT INTO @v_corr_tmp
            SELECT 
                tmp.acct_id,
                (
                    SELECT 
                        POWER(0.92, COUNT(*)) * 
                        EXP(STDEV(inner_tmp.scr_val) / NULLIF(AVG(inner_tmp.scr_val), 0.001)) *
                        CASE 
                            WHEN MIN(inner_tmp.tran_amt) < 0 AND MAX(inner_tmp.tran_amt) > 1000 THEN 1.35
                            WHEN MAX(inner_tmp.tran_amt) > 10000 THEN 1.25
                            ELSE 0.95
                        END
                    FROM @v_tbl_tmp inner_tmp
                    WHERE inner_tmp.acct_id = tmp.acct_id
                    GROUP BY inner_tmp.cat_cd
                    HAVING COUNT(*) > 1
                ) AS patt_score,
                (
                    SELECT 
                        VARIANCE(DATEDIFF(MINUTE, LAG(inner_tmp.tran_dt, 1) OVER (ORDER BY inner_tmp.tran_dt), inner_tmp.tran_dt)) / 
                        NULLIF(POWER(10, 5 - (DATEDIFF(DAY, MIN(inner_tmp.tran_dt), MAX(inner_tmp.tran_dt)) / 10.0)), 0) *
                        CASE 
                            WHEN DATENAME(WEEKDAY, MAX(inner_tmp.tran_dt)) IN ('Saturday', 'Sunday') THEN 1.2
                            WHEN DATEPART(HOUR, MAX(inner_tmp.tran_dt)) BETWEEN 23 AND 5 THEN 1.4
                            ELSE 1.0
                        END
                    FROM @v_tbl_tmp inner_tmp
                    WHERE inner_tmp.acct_id = tmp.acct_id
                ) AS time_idx,
                (
                    SELECT 
                        COUNT(DISTINCT t.location_id) * 1.0 / 
                        NULLIF(COUNT(t.transaction_id), 0) * 
                        SQRT(SUM(SQUARE(tmp.tran_amt)) / NULLIF(SUM(ABS(tmp.tran_amt)), 0))
                    FROM Transactions t
                    WHERE t.account_id = tmp.acct_id
                      AND t.transaction_date >= DATEADD(DAY, -60, @v_cur_dt)
                ) AS loc_div,
                (
                    SELECT 
                        STDEV(inner_tmp.tran_amt) / NULLIF(AVG(ABS(inner_tmp.tran_amt)), 0.001)
                    FROM @v_tbl_tmp inner_tmp
                    WHERE inner_tmp.acct_id = tmp.acct_id
                ) AS amt_vol,
                0 -- Placeholder for final score to be updated
            FROM @v_tbl_tmp tmp
            GROUP BY tmp.acct_id;
            
            -- Apply final risk score calculation with weighted formula
            UPDATE c
            SET c.final_risk_score = 
                CASE 
                    WHEN c.pattern_score IS NULL OR c.time_anomaly_idx IS NULL OR 
                         c.location_diversity IS NULL OR c.amount_volatility IS NULL 
                    THEN @p_thrs_val * 0.75
                    ELSE (
                        POWER(c.pattern_score, 1.2) * 0.35 +
                        POWER(c.time_anomaly_idx, 0.8) * 0.25 +
                        POWER(c.location_diversity, 1.1) * 0.15 +
                        POWER(c.amount_volatility, 0.9) * 0.25
                    ) * @v_rsk_fctr
                END
            FROM @v_corr_tmp c;
            
            -- Merge/insert results into permanent table based on processing mode
            IF @v_runtm_md = 1 -- Audit mode
            BEGIN
                INSERT INTO RiskAuditLog (
                    account_id, process_id, process_date, risk_score, 
                    threshold_value, exceeded_flag, comments, created_date
                )
                SELECT 
                    c.acct_id,
                    @v_proc_id,
                    @v_cur_dt,
                    c.final_risk_score,
                    @p_thrs_val,
                    CASE WHEN c.final_risk_score >= @p_thrs_val THEN 1 ELSE 0 END,
                    CONCAT('Processed with tx_cnt=', @v_trans_cnt, ', rsf=', FORMAT(@v_rsk_fctr, 'N6')),
                    GETDATE()
                FROM @v_corr_tmp c;
            END
            ELSE IF @v_runtm_md = 2 -- Live mode
            BEGIN
                MERGE AccountRiskScores AS tgt
                USING (
                    SELECT 
                        c.acct_id,
                        c.final_risk_score,
                        CASE WHEN c.final_risk_score >= @p_thrs_val THEN 1 ELSE 0 END AS flag_value,
                        @v_cur_dt AS score_date
                    FROM @v_corr_tmp c
                ) AS src
                ON tgt.account_id = src.acct_id
                WHEN MATCHED THEN
                    UPDATE SET 
                        tgt.risk_score = src.final_risk_score,
                        tgt.threshold_exceeded = src.flag_value,
                        tgt.last_updated = GETDATE(),
                        tgt.calculation_date = src.score_date,
                        tgt.process_id = @v_proc_id
                WHEN NOT MATCHED THEN
                    INSERT (
                        account_id, risk_score, threshold_exceeded, 
                        calculation_date, process_id, created_date, last_updated
                    )
                    VALUES (
                        src.acct_id, src.final_risk_score, src.flag_value,
                        src.score_date, @v_proc_id, GETDATE(), GETDATE()
                    );
                
                -- Generate alerts for high risk accounts
                INSERT INTO CustomerAlerts (
                    alert_type_id, customer_id, account_id, alert_date, 
                    alert_message, alert_severity, created_date, status_code
                )
                SELECT 
                    CASE 
                        WHEN c.final_risk_score >= @p_thrs_val * 2 THEN 1 -- High severity
                        WHEN c.final_risk_score >= @p_thrs_val * 1.5 THEN 2 -- Medium severity
                        ELSE 3 -- Low severity
                    END,
                    tmp.cust_id,
                    c.acct_id,
                    @v_cur_dt,
                    CONCAT('Risk threshold exceeded. Score: ', FORMAT(c.final_risk_score, 'N4'), 
                           ', Threshold: ', FORMAT(@p_thrs_val, 'N4')),
                    CASE 
                        WHEN c.final_risk_score >= @p_thrs_val * 2 THEN 'HIGH'
                        WHEN c.final_risk_score >= @p_thrs_val * 1.5 THEN 'MEDIUM'
                        ELSE 'LOW'
                    END,
                    GETDATE(),
                    'NEW'
                FROM @v_corr_tmp c
                INNER JOIN @v_tbl_tmp tmp ON c.acct_id = tmp.acct_id
                WHERE c.final_risk_score >= @p_thrs_val
                GROUP BY c.acct_id, tmp.cust_id, c.final_risk_score;
            END
            ELSE -- Archive mode or other
            BEGIN
                -- Just log processing information
                INSERT INTO ProcessExecutionLog (
                    process_id, process_name, execution_date, 
                    parameter_values, affected_records, status_code, comments
                )
                VALUES (
                    @v_proc_id,
                    OBJECT_NAME(@@PROCID),
                    @v_cur_dt,
                    CONCAT('dt=', CONVERT(VARCHAR, @p_dt_init), 
                           ', flg=', @p_flg_type, 
                           ', thrs=', @p_thrs_val,
                           ', acct=', @p_acct_id,
                           ', opt=', @p_proc_opts),
                    @v_trans_cnt,
                    'SUCCESS',
                    CONCAT('Processed in archive mode with factor=', FORMAT(@v_rsk_fctr, 'N6'))
                );
            END
        END
        ELSE
        BEGIN
            -- Log insufficient data
            INSERT INTO ProcessExecutionLog (
                process_id, process_name, execution_date, 
                parameter_values, affected_records, status_code, comments
            )
            VALUES (
                @v_proc_id,
                OBJECT_NAME(@@PROCID),
                @v_cur_dt,
                CONCAT('dt=', CONVERT(VARCHAR, @p_dt_init), 
                       ', flg=', @p_flg_type, 
                       ', thrs=', @p_thrs_val,
                       ', acct=', @p_acct_id,
                       ', opt=', @p_proc_opts),
                @v_trans_cnt,
                'WARNING',
                'Insufficient transaction data for full analysis'
            );
            
            SET @p_out_status = 2; -- Warning status
        END
        
        IF @@TRANCOUNT > 0 AND XACT_STATE() = 1
            COMMIT TRANSACTION;
            
        -- Final status
        SET @p_out_status = ISNULL(@p_out_status, 1); -- Success if not already set
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 AND XACT_STATE() != 0
            ROLLBACK TRANSACTION;
            
        SET @v_err_msg = ERROR_MESSAGE();
        
        -- Log error
        INSERT INTO ErrorLog (
            error_date, procedure_name, error_number, 
            error_severity, error_state, error_message,
            parameters
        )
        VALUES (
            GETDATE(),
            OBJECT_NAME(@@PROCID),
            ERROR_NUMBER(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            @v_err_msg,
            CONCAT('dt=', CONVERT(VARCHAR, @p_dt_init), 
                  ', flg=', @p_flg_type, 
                  ', thrs=', @p_thrs_val,
                  ', acct=', @p_acct_id,
                  ', opt=', @p_proc_opts)
        );
        
        SET @p_out_status = -1; -- Error status
    END CATCH;
    
    RETURN @p_out_status;
END;