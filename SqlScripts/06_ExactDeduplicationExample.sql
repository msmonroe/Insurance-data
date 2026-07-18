WITH ExactCandidates AS
(
    SELECT
        LeadId,
        HASHBYTES(
            'SHA2_256',
            CONCAT_WS('|',
                FirstName,
                MiddleInitial,
                LastName,
                NameSuffix,
                DateOfBirth,
                Gender,
                Email,
                Phone,
                Address1,
                Address2,
                City,
                County,
                State,
                ZipCode,
                Occupation,
                MaritalStatus,
                AnnualIncome,
                HomeOwner,
                TobaccoUser,
                InsuranceType,
                LeadSource,
                CampaignCode,
                VendorCode,
                ConsentToContact,
                CreatedDate,
                ModifiedDate
            )
        ) AS LeadHash
    FROM dbo.RawInsuranceLead
)
SELECT LeadHash, COUNT(*) AS DuplicateCount, MIN(LeadId) AS SuggestedPrimaryLeadId
FROM ExactCandidates
GROUP BY LeadHash
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;
GO
