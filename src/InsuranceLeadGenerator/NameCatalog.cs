using System.Globalization;
using System.Text;

namespace InsuranceData;

internal sealed class NameCatalog
{
    private static readonly string[] MaleNames =
    {
        "James", "Robert", "Michael", "William", "David", "Joseph", "Christopher", "Matthew", "Anthony", "Jonathan",
        "José", "René", "André", "Miguel", "Elijah", "Daniel", "Jacob", "Alexander", "Noah", "Benjamin",
    };

    private static readonly string[] FemaleNames =
    {
        "Mary", "Patricia", "Jennifer", "Elizabeth", "Linda", "Barbara", "Susan", "Jessica", "Sarah", "Karen",
        "Renée", "Chloë", "María", "Ana", "Katherine", "Emily", "Madison", "Ashley", "Emma", "Sophia",
    };

    private static readonly string[] NeutralNames =
    {
        "Taylor", "Jordan", "Casey", "Avery", "Morgan", "Peyton", "Riley", "Jamie", "Cameron", "Quinn",
    };

    private static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
        "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
        "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
        "Green", "Adams", "Nelson", "Baker", "Rivera", "Campbell", "Mitchell", "Carter", "Muñoz", "O'Neil", "Smith-Jones",
    };

    private static readonly string[] Suffixes = { "", "", "", "Jr", "Sr", "II", "III" };
    private static readonly string[] Occupations =
    {
        "Account Manager", "Administrative Assistant", "Automotive Technician", "Bookkeeper", "Business Analyst",
        "Claims Specialist", "Construction Supervisor", "Dental Hygienist", "Electrician", "Financial Analyst",
        "Graphic Designer", "HVAC Technician", "IT Support Specialist", "Licensed Practical Nurse", "Operations Manager",
        "Paralegal", "Pharmacy Technician", "Project Coordinator", "Registered Nurse", "Sales Consultant",
        "School Counselor", "Small Business Owner", "Teacher", "Warehouse Lead", "Welder",
    };

    private static readonly Dictionary<string, string[]> Nicknames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Robert"] = new[] { "Bob", "Rob", "Bobby" },
        ["William"] = new[] { "Bill", "Will", "Billy" },
        ["Elizabeth"] = new[] { "Liz", "Beth", "Eliza" },
        ["Jennifer"] = new[] { "Jen", "Jenny" },
        ["Michael"] = new[] { "Mike", "Mikey" },
        ["Christopher"] = new[] { "Chris" },
        ["Katherine"] = new[] { "Kate", "Katie" },
        ["Matthew"] = new[] { "Matt" },
        ["Jonathan"] = new[] { "Jon", "Johnny" },
        ["Anthony"] = new[] { "Tony" },
    };

    public NameParts CreateName(DeterministicRandom random)
    {
        Gender gender = PickGender(random);
        string firstName = gender switch
        {
            Gender.M => random.NextItem(MaleNames),
            Gender.F => random.NextItem(FemaleNames),
            _ => random.NextItem(NeutralNames),
        };

        return new NameParts(
            firstName,
            random.NextBool(0.72d) ? ((char)('A' + random.NextInt(26))).ToString() : string.Empty,
            random.NextItem(LastNames),
            random.NextItem(Suffixes),
            gender.ToString());
    }

    public string CreateOccupation(DeterministicRandom random) => random.NextItem(Occupations);

    public string CreateEmail(string firstName, string lastName, DeterministicRandom random)
    {
        string localFirst = NormalizeForEmail(firstName);
        string localLast = NormalizeForEmail(lastName);
        if (string.IsNullOrWhiteSpace(localFirst) || string.IsNullOrWhiteSpace(localLast))
        {
            return string.Empty;
        }

        string[] domains = { "example.test", "mail.test", "insurance-leads.test", "example.org", "example.net", "example.com" };
        string separator = random.NextItem(new[] { ".", ".", "", "_" });
        string suffix = random.NextBool(0.35d) ? random.NextInt(10, 999).ToString(CultureInfo.InvariantCulture) : string.Empty;
        return $"{localFirst}{separator}{localLast}{suffix}@{random.NextItem(domains)}";
    }

    public string CreateNickname(string firstName, DeterministicRandom random)
    {
        if (Nicknames.TryGetValue(firstName, out string[]? values) && values.Length > 0)
        {
            return random.NextItem(values);
        }

        return firstName;
    }

    public static string NormalizeForEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static Gender PickGender(DeterministicRandom random)
    {
        int sample = random.NextInt(100);
        if (sample < 48)
        {
            return Gender.M;
        }

        if (sample < 96)
        {
            return Gender.F;
        }

        return Gender.U;
    }
}

internal sealed record NameParts(string FirstName, string MiddleInitial, string LastName, string Suffix, string Gender);
