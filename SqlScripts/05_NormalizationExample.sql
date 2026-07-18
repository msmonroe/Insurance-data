CREATE OR ALTER VIEW dbo.vNormalizedInsuranceLead
AS
SELECT
    TRY_CONVERT(BIGINT, LeadId) AS LeadId,
    SourceLeadId,
    NULLIF(LTRIM(RTRIM(FirstName)), '') AS FirstName,
    NULLIF(LTRIM(RTRIM(MiddleInitial)), '') AS MiddleInitial,
    NULLIF(LTRIM(RTRIM(LastName)), '') AS LastName,
    NULLIF(LTRIM(RTRIM(NameSuffix)), '') AS NameSuffix,
    TRY_CONVERT(date, DateOfBirth) AS DateOfBirth,
    UPPER(NULLIF(LTRIM(RTRIM(Gender)), '')) AS Gender,
    LOWER(NULLIF(LTRIM(RTRIM(Email)), '')) AS Email,
    TRANSLATE(NULLIF(Phone, ''), '().- +', '') AS NormalizedPhone,
    UPPER(NULLIF(LTRIM(RTRIM(Address1)), '')) AS Address1,
    UPPER(NULLIF(LTRIM(RTRIM(Address2)), '')) AS Address2,
    UPPER(NULLIF(LTRIM(RTRIM(City)), '')) AS City,
    UPPER(NULLIF(LTRIM(RTRIM(County)), '')) AS County,
    UPPER(NULLIF(LTRIM(RTRIM(State)), '')) AS State,
    LEFT(NULLIF(LTRIM(RTRIM(ZipCode)), ''), 5) AS Zip5,
    TRY_CONVERT(DECIMAL(18,2), AnnualIncome) AS AnnualIncome,
    UPPER(NULLIF(LTRIM(RTRIM(InsuranceType)), '')) AS InsuranceType,
    DuplicateType,
    TRY_CONVERT(BIGINT, OriginalLeadId) AS OriginalLeadId,
    ImportBatchId,
    SourceFileName,
    SourceRowNumber,
    ImportedUtc
FROM dbo.RawInsuranceLead;
GO
