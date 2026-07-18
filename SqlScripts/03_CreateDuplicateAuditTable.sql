CREATE TABLE dbo.DuplicateAudit
(
    DuplicateAuditId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ImportBatchId BIGINT NOT NULL,
    LeadId BIGINT NOT NULL,
    OriginalLeadId BIGINT NULL,
    DuplicateType NVARCHAR(50) NOT NULL,
    MatchCategory NVARCHAR(100) NOT NULL,
    MatchScore DECIMAL(9,4) NULL,
    ProcessedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_DuplicateAudit_ProcessedUtc DEFAULT SYSUTCDATETIME(),
    Notes NVARCHAR(2000) NULL,
    CONSTRAINT FK_DuplicateAudit_ImportBatch FOREIGN KEY (ImportBatchId) REFERENCES dbo.ImportBatch(ImportBatchId)
);
GO

CREATE INDEX IX_DuplicateAudit_LeadId ON dbo.DuplicateAudit(LeadId);
CREATE INDEX IX_DuplicateAudit_OriginalLeadId ON dbo.DuplicateAudit(OriginalLeadId);
GO
