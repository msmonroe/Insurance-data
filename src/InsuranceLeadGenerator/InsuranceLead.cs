namespace InsuranceData;

internal sealed class InsuranceLead
{
    public long LeadId { get; set; }
    public string SourceLeadId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleInitial { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NameSuffix { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string MaritalStatus { get; set; } = string.Empty;
    public string AnnualIncome { get; set; } = string.Empty;
    public string HomeOwner { get; set; } = string.Empty;
    public string TobaccoUser { get; set; } = string.Empty;
    public string InsuranceType { get; set; } = string.Empty;
    public string LeadSource { get; set; } = string.Empty;
    public string CampaignCode { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public string ConsentToContact { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string ModifiedDate { get; set; } = string.Empty;
    public DuplicateType DuplicateType { get; set; }
    public long? OriginalLeadId { get; set; }

    public static IReadOnlyList<string> HeaderColumns { get; } = new[]
    {
        "LeadId",
        "SourceLeadId",
        "FirstName",
        "MiddleInitial",
        "LastName",
        "NameSuffix",
        "DateOfBirth",
        "Gender",
        "Email",
        "Phone",
        "Address1",
        "Address2",
        "City",
        "County",
        "State",
        "ZipCode",
        "Occupation",
        "MaritalStatus",
        "AnnualIncome",
        "HomeOwner",
        "TobaccoUser",
        "InsuranceType",
        "LeadSource",
        "CampaignCode",
        "VendorCode",
        "ConsentToContact",
        "CreatedDate",
        "ModifiedDate",
        "DuplicateType",
        "OriginalLeadId",
    };

    public InsuranceLead Clone() => new()
    {
        LeadId = LeadId,
        SourceLeadId = SourceLeadId,
        FirstName = FirstName,
        MiddleInitial = MiddleInitial,
        LastName = LastName,
        NameSuffix = NameSuffix,
        DateOfBirth = DateOfBirth,
        Gender = Gender,
        Email = Email,
        Phone = Phone,
        Address1 = Address1,
        Address2 = Address2,
        City = City,
        County = County,
        State = State,
        ZipCode = ZipCode,
        Occupation = Occupation,
        MaritalStatus = MaritalStatus,
        AnnualIncome = AnnualIncome,
        HomeOwner = HomeOwner,
        TobaccoUser = TobaccoUser,
        InsuranceType = InsuranceType,
        LeadSource = LeadSource,
        CampaignCode = CampaignCode,
        VendorCode = VendorCode,
        ConsentToContact = ConsentToContact,
        CreatedDate = CreatedDate,
        ModifiedDate = ModifiedDate,
        DuplicateType = DuplicateType,
        OriginalLeadId = OriginalLeadId,
    };

    public bool BusinessFieldsEqual(InsuranceLead other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return SourceLeadId == other.SourceLeadId
            && FirstName == other.FirstName
            && MiddleInitial == other.MiddleInitial
            && LastName == other.LastName
            && NameSuffix == other.NameSuffix
            && DateOfBirth == other.DateOfBirth
            && Gender == other.Gender
            && Email == other.Email
            && Phone == other.Phone
            && Address1 == other.Address1
            && Address2 == other.Address2
            && City == other.City
            && County == other.County
            && State == other.State
            && ZipCode == other.ZipCode
            && Occupation == other.Occupation
            && MaritalStatus == other.MaritalStatus
            && AnnualIncome == other.AnnualIncome
            && HomeOwner == other.HomeOwner
            && TobaccoUser == other.TobaccoUser
            && InsuranceType == other.InsuranceType
            && LeadSource == other.LeadSource
            && CampaignCode == other.CampaignCode
            && VendorCode == other.VendorCode
            && ConsentToContact == other.ConsentToContact
            && CreatedDate == other.CreatedDate
            && ModifiedDate == other.ModifiedDate;
    }
}
