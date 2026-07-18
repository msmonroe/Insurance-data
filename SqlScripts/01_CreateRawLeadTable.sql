CREATE TABLE dbo.RawInsuranceLead
(
    RawInsuranceLeadId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    LeadId VARCHAR(32) NULL,
    SourceLeadId NVARCHAR(64) NULL,
    FirstName NVARCHAR(200) NULL,
    MiddleInitial NVARCHAR(50) NULL,
    LastName NVARCHAR(200) NULL,
    NameSuffix NVARCHAR(50) NULL,
    DateOfBirth NVARCHAR(50) NULL,
    Gender NVARCHAR(20) NULL,
    Email NVARCHAR(320) NULL,
    Phone NVARCHAR(50) NULL,
    Address1 NVARCHAR(400) NULL,
    Address2 NVARCHAR(400) NULL,
    City NVARCHAR(200) NULL,
    County NVARCHAR(200) NULL,
    State NVARCHAR(20) NULL,
    ZipCode NVARCHAR(20) NULL,
    Occupation NVARCHAR(200) NULL,
    MaritalStatus NVARCHAR(50) NULL,
    AnnualIncome NVARCHAR(50) NULL,
    HomeOwner NVARCHAR(20) NULL,
    TobaccoUser NVARCHAR(20) NULL,
    InsuranceType NVARCHAR(50) NULL,
    LeadSource NVARCHAR(50) NULL,
    CampaignCode NVARCHAR(50) NULL,
    VendorCode NVARCHAR(50) NULL,
    ConsentToContact NVARCHAR(20) NULL,
    CreatedDate NVARCHAR(50) NULL,
    ModifiedDate NVARCHAR(50) NULL,
    DuplicateType NVARCHAR(50) NULL,
    OriginalLeadId VARCHAR(32) NULL,
    ImportBatchId BIGINT NOT NULL CONSTRAINT DF_RawInsuranceLead_ImportBatchId DEFAULT (0),
    SourceFileName NVARCHAR(260) NOT NULL CONSTRAINT DF_RawInsuranceLead_SourceFileName DEFAULT (N''),
    SourceRowNumber BIGINT NULL,
    ImportedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_RawInsuranceLead_ImportedUtc DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_RawInsuranceLead_ImportBatchId ON dbo.RawInsuranceLead(ImportBatchId, SourceRowNumber);
CREATE INDEX IX_RawInsuranceLead_LeadId ON dbo.RawInsuranceLead(LeadId);
GO
