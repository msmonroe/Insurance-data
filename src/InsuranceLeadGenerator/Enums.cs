namespace InsuranceData;

internal enum DuplicateType
{
    UNIQUE,
    EXACT,
    EMAIL_CHANGED,
    PHONE_CHANGED,
    ADDRESS_CHANGED,
    NAME_VARIATION,
    FORMATTING_VARIATION,
    MULTIPLE_FIELDS_CHANGED,
    INTENTIONALLY_INVALID,
}

internal enum InsuranceType
{
    Auto,
    Home,
    Renters,
    Life,
    Health,
    Medicare,
    Supplemental,
    CommercialAuto,
    SmallBusiness,
    Umbrella,
}

internal enum LeadSource
{
    WEB_FORM,
    PARTNER,
    CALL_CENTER,
    PURCHASED_LIST,
    REFERRAL,
    LANDING_PAGE,
    SOCIAL_MEDIA,
    DIRECT_MAIL,
    EVENT,
    INTERNAL,
}

internal enum Gender
{
    M,
    F,
    U,
}

internal enum MaritalStatus
{
    Single,
    Married,
    Divorced,
    Widowed,
    DomesticPartner,
}
