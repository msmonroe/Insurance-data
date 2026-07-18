SELECT DuplicateType, COUNT(*) AS GeneratedCount
FROM dbo.RawInsuranceLead
GROUP BY DuplicateType
ORDER BY DuplicateType;
GO

SELECT
    COUNT(*) AS TotalRows,
    COUNT(DISTINCT TRY_CONVERT(BIGINT, LeadId)) AS DistinctLeadIds,
    SUM(CASE WHEN DuplicateType = 'UNIQUE' AND NULLIF(OriginalLeadId, '') IS NOT NULL THEN 1 ELSE 0 END) AS UnexpectedOriginalLeadIdOnUnique,
    SUM(CASE WHEN DuplicateType <> 'UNIQUE' AND DuplicateType <> 'INTENTIONALLY_INVALID' AND NULLIF(OriginalLeadId, '') IS NULL THEN 1 ELSE 0 END) AS MissingOriginalLeadIdOnDuplicate
FROM dbo.RawInsuranceLead;
GO

SELECT
    DuplicateType,
    COUNT(*) AS GeneratedRows,
    SUM(CASE WHEN TRY_CONVERT(BIGINT, OriginalLeadId) IS NOT NULL THEN 1 ELSE 0 END) AS RowsWithOriginalLeadId
FROM dbo.RawInsuranceLead
GROUP BY DuplicateType
ORDER BY GeneratedRows DESC;
GO
