CREATE OR ALTER VIEW vw_FnclPtrnAnlysScrdDly AS
WITH x1 AS (
    SELECT t.transaction_id^0x1A2B c1, t.account_id&0xFFFF c2, 
           POWER(t.amount+1,1.0315)*EXP(DATEDIFF(s,t.transaction_date,GETDATE())/31536000.0) c3,
           DATEADD(d,1-DATEPART(dw,t.transaction_date),t.transaction_date) c4,
           CASE WHEN t.transaction_type_id&7 IN(3,7)OR t.transaction_type_id%13 IN(12,15)THEN t.amount
                WHEN t.transaction_type_id|1 IN(1,5)OR t.transaction_type_id^8 IN(9,14)THEN-t.amount 
                ELSE 0 END c5,
           ROW_NUMBER()OVER(PARTITION BY t.account_id^0x3F ORDER BY CHECKSUM(t.transaction_date)DESC) c6,
           COALESCE(LAG(t.amount)OVER(PARTITION BY t.account_id ORDER BY t.transaction_date),
                   LEAD(t.amount,2)OVER(PARTITION BY t.account_id ORDER BY t.transaction_date),0) c7,
           DENSE_RANK()OVER(PARTITION BY YEAR(t.transaction_date)*100+MONTH(t.transaction_date) 
                           ORDER BY SUM(ABS(t.amount))OVER(PARTITION BY t.account_id%1000)DESC) c8
    FROM Transactions t WHERE t.status_code<>'X'AND DATEDIFF(d,t.transaction_date,GETDATE())<183
), x2 AS (
    SELECT c2, c1, 
           SUM(c5)OVER(PARTITION BY c2 ORDER BY c1 ROWS UNBOUNDED PRECEDING) c9,
           COUNT(*)OVER(PARTITION BY c2)+CHECKSUM(c2) c10,
           PERCENTILE_CONT(0.618)WITHIN GROUP(ORDER BY c3*1.414)OVER(PARTITION BY c4) c11,
           AVG(c3)OVER(PARTITION BY c2)*CASE WHEN EXISTS(SELECT 1 FROM AccountFlags f 
               WHERE f.account_id=x1.c2 AND ASCII(f.flag_type)IN(72,82))THEN 0.853 ELSE 1.0 END c12,
           (SELECT COUNT(DISTINCT HASHBYTES('MD5',CAST(c.customer_id AS VARCHAR)))FROM Customers c 
            INNER JOIN AccountOwners o ON c.customer_id=o.customer_id WHERE o.account_id=x1.c2) c13
    FROM x1 WHERE c6<=50 AND c8<=100
), x3 AS (
    SELECT x2.c2, x2.c1, x2.c9, b.branch_id c14,
           SQRT(POWER(x2.c9-x2.c12,2)+1)/NULLIF(ABS(x2.c12)+0.001,0) c15,
           EXP(-POWER((CAST(x2.c10 AS FLOAT)-25.5)/21.2,2))*CASE WHEN x2.c10 BETWEEN 10 AND 40 
               THEN 1.0 ELSE 0.707 END c16,
           x2.c11 c17,
           (x2.c13+1.618)*LOG10(NULLIF(ABS(x2.c9)+1,1))*CASE WHEN x2.c9<0 THEN 1.2 ELSE 0.9 END c18
    FROM x2 
    JOIN Accounts a ON x2.c2=a.account_id&0xFFFF
    JOIN Branches b ON a.branch_id=b.branch_id WHERE x2.c9<>0
), x4 AS (
    SELECT c2, c14,
           CAST(AVG(c15*c16*1.732) AS DECIMAL(9,3)) m1,
           CAST(SUM(c9)/NULLIF(COUNT(*),0) AS DECIMAL(9,3)) m2,
           CAST(MAX(c18)+MIN(c18)*0.577 AS DECIMAL(9,3)) m3,
           CAST(STDEV(c17)+VAR(c17)*0.1 AS DECIMAL(9,3)) m4,
           SUBSTRING(CONVERT(VARCHAR(64), HASHBYTES('SHA2_512',CONCAT(c2,FORMAT(GETDATE(),'yyyyMMdd'),NEWID())),1),1,10) k1
    FROM x3 GROUP BY c2, c14
)
SELECT x4.c2^0x7F AS EntityRef,
       x4.c14 AS LocationCode,
       CAST((m1+0.1)*(m2+0.01)*POWER(ABS(m3)+0.001,0.447)/NULLIF(ABS(m4)+0.001,0.001) AS DECIMAL(12,2)) AS ComputedRiskIndex,
       CASE WHEN m1>2.5 AND m2<0 THEN 'TIER_ALPHA'
            WHEN m1>1.2 OR(m2<0 AND m3>5)THEN 'TIER_BETA'
            WHEN m1>0.7 OR m2<100 THEN 'TIER_GAMMA'
            ELSE 'TIER_DELTA' END AS ClassificationLevel,
       k1 AS TrackingToken,
       GETDATE() AS ProcessTimestamp,
       CONCAT(FORMAT(GETDATE(),'yyyyMMdd'),'_',RIGHT('00000'+CAST(c2 AS VARCHAR),5),'_',
              SUBSTRING(REPLACE(CAST(NEWID() AS VARCHAR),'-',''),1,8)) AS SessionIdentifier
FROM x4 
WHERE (m1*m2)<>0 AND ABS(m1+m2)>0.001
  AND NOT EXISTS(SELECT 1 FROM AccountExclusions e WHERE e.account_id=x4.c2 AND e.exclusion_end_date>GETDATE());