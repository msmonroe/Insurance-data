DECLARE @FilePath NVARCHAR(4000) = N'D:\LeadData\insurance_leads_001.csv';
DECLARE @ErrorFile NVARCHAR(4000) = N'D:\LeadData\errors\insurance_leads_001_error.log';
DECLARE @ImportBatchId BIGINT;
DECLARE @BulkSql NVARCHAR(MAX);

INSERT dbo.ImportBatch (BatchName, SourceFileName, ExpectedRowCount)
VALUES (N'Insurance Lead Import', N'insurance_leads_001.csv', 5000000);

SET @ImportBatchId = SCOPE_IDENTITY();

SET @BulkSql = N'
BULK INSERT dbo.RawInsuranceLead
FROM ''' + REPLACE(@FilePath, '''', '''''') + N'''
WITH
(
    FORMAT = ''CSV'',
    CODEPAGE = ''65001'',
    FIRSTROW = 2,
    FIELDQUOTE = ''"'',
    FIELDTERMINATOR = '','',
    ROWTERMINATOR = ''0x0A'',
    TABLOCK,
    BATCHSIZE = 100000,
    ERRORFILE = ''' + REPLACE(@ErrorFile, '''', '''''') + N''',
    KEEPNULLS
);';

EXEC sys.sp_executesql @BulkSql;

UPDATE dbo.RawInsuranceLead
SET ImportBatchId = @ImportBatchId,
    SourceFileName = N'insurance_leads_001.csv',
    SourceRowNumber = RowNumbering.SourceRowNumber,
    ImportedUtc = SYSUTCDATETIME()
FROM dbo.RawInsuranceLead AS target
INNER JOIN
(
    SELECT RawInsuranceLeadId, ROW_NUMBER() OVER (ORDER BY RawInsuranceLeadId) AS SourceRowNumber
    FROM dbo.RawInsuranceLead
    WHERE ImportBatchId = 0
) AS RowNumbering
    ON RowNumbering.RawInsuranceLeadId = target.RawInsuranceLeadId
WHERE target.ImportBatchId = 0;

UPDATE dbo.ImportBatch
SET ImportedRowCount = @@ROWCOUNT,
    CompletedUtc = SYSUTCDATETIME(),
    Status = N'COMPLETED'
WHERE ImportBatchId = @ImportBatchId;
GO

-- bcp example:
-- bcp InsuranceDb.dbo.RawInsuranceLead in D:\LeadData\insurance_leads_001.csv -S localhost -d InsuranceDb -T -c -C 65001 -t, -r\n -F 2 -b 100000 -e D:\LeadData\errors\insurance_leads_001.bcp.err
