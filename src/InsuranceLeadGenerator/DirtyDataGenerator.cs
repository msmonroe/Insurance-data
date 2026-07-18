using System.Globalization;

namespace InsuranceData;

internal sealed class DirtyDataGenerator
{
    public void Apply(InsuranceLead lead, int seed, bool includeStructuralErrors)
    {
        var random = new DeterministicRandom(seed, lead.LeadId);
        lead.DuplicateType = DuplicateType.INTENTIONALLY_INVALID;
        lead.OriginalLeadId = null;

        switch (random.NextInt(12))
        {
            case 0:
                lead.Email = "missingatsymbol.example.test";
                break;
            case 1:
                lead.Phone = "555-12ABCD";
                break;
            case 2:
                lead.DateOfBirth = "2023-02-30";
                break;
            case 3:
                lead.DateOfBirth = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                break;
            case 4:
                lead.State = "ZZ";
                break;
            case 5:
                lead.ZipCode = "9A12B";
                break;
            case 6:
                lead.AnnualIncome = "-45000";
                break;
            case 7:
                lead.AnnualIncome = "9999999999";
                break;
            case 8:
                lead.FirstName = string.Empty;
                lead.LastName = random.NextBool() ? string.Empty : lead.LastName;
                break;
            case 9:
                lead.Address1 = new string('X', 180) + ", Apt \"Overflow\"";
                lead.Address2 = "Floor 9\nSuite 900";
                break;
            case 10:
                lead.FirstName = "NULL";
                lead.LastName = "UNKNOWN";
                lead.Email = "N/A";
                break;
            default:
                lead.MiddleInitial = "\t";
                lead.Address2 = "Line1\r\nLine2, \"Quoted\"";
                lead.CampaignCode = includeStructuralErrors ? "BROKEN,FIELD,COUNT" : "BROKEN\tCODE";
                break;
        }
    }
}
