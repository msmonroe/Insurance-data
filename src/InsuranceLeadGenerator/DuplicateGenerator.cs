using System.Globalization;
using System.Text;

namespace InsuranceData;

internal sealed class DuplicateGenerator
{
    private readonly NameCatalog _nameCatalog;

    public DuplicateGenerator(NameCatalog nameCatalog)
    {
        _nameCatalog = nameCatalog;
    }

    public InsuranceLead CreateExactDuplicate(InsuranceLead original, long newLeadId, long originalLeadId)
    {
        InsuranceLead duplicate = original.Clone();
        duplicate.LeadId = newLeadId;
        duplicate.DuplicateType = DuplicateType.EXACT;
        duplicate.OriginalLeadId = originalLeadId;
        return duplicate;
    }

    public InsuranceLead CreateModifiedDuplicate(InsuranceLead original, long newLeadId, long originalLeadId, int seed)
    {
        InsuranceLead duplicate = original.Clone();
        duplicate.LeadId = newLeadId;
        duplicate.OriginalLeadId = originalLeadId;

        var random = new DeterministicRandom(seed, newLeadId);
        DuplicateType duplicateType = PickModifiedDuplicateType(random);
        duplicate.DuplicateType = duplicateType;

        switch (duplicateType)
        {
            case DuplicateType.EMAIL_CHANGED:
                duplicate.Email = MutateEmail(original, random);
                break;
            case DuplicateType.PHONE_CHANGED:
                duplicate.Phone = MutatePhone(original.Phone, random);
                break;
            case DuplicateType.ADDRESS_CHANGED:
                MutateAddress(duplicate, random);
                break;
            case DuplicateType.NAME_VARIATION:
                MutateName(duplicate, random);
                break;
            case DuplicateType.FORMATTING_VARIATION:
                MutateFormatting(duplicate, random);
                break;
            case DuplicateType.MULTIPLE_FIELDS_CHANGED:
                MutateName(duplicate, random);
                duplicate.Email = MutateEmail(original, random);
                duplicate.Phone = MutatePhone(original.Phone, random);
                MutateAddress(duplicate, random);
                break;
            default:
                throw new InvalidOperationException($"Unexpected duplicate type {duplicateType}.");
        }

        EnsureVariation(original, duplicate, duplicateType, random);
        duplicate.ModifiedDate = BumpTimestamp(original.ModifiedDate, random);
        return duplicate;
    }

    private static DuplicateType PickModifiedDuplicateType(DeterministicRandom random)
    {
        int sample = random.NextInt(100);
        return sample switch
        {
            < 20 => DuplicateType.EMAIL_CHANGED,
            < 40 => DuplicateType.PHONE_CHANGED,
            < 60 => DuplicateType.ADDRESS_CHANGED,
            < 80 => DuplicateType.NAME_VARIATION,
            < 90 => DuplicateType.FORMATTING_VARIATION,
            _ => DuplicateType.MULTIPLE_FIELDS_CHANGED,
        };
    }

    private string MutateEmail(InsuranceLead original, DeterministicRandom random)
    {
        if (string.IsNullOrWhiteSpace(original.Email))
        {
            return _nameCatalog.CreateEmail(original.FirstName, original.LastName, random);
        }

        string email = original.Email;
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            return _nameCatalog.CreateEmail(original.FirstName, original.LastName, random);
        }

        string local = email[..atIndex];
        string domain = email[(atIndex + 1)..];
        return random.NextInt(7) switch
        {
            0 => email.ToUpperInvariant(),
            1 => $" {email}",
            2 => email.Replace(".", string.Empty, StringComparison.Ordinal),
            3 => local.Length > 1 ? $"{local[..1]}{local[2..]}@{domain}" : _nameCatalog.CreateEmail(original.FirstName, original.LastName, random),
            4 => $"{local}.{random.NextInt(1, 9).ToString(CultureInfo.InvariantCulture)}@mail.test",
            5 => string.Empty,
            _ => $"{local}@examplemail.test",
        };
    }

    private static string MutatePhone(string phone, DeterministicRandom random)
    {
        string digits = new(phone.Where(char.IsDigit).ToArray());
        if (digits.Length >= 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        if (digits.Length < 10)
        {
            digits = "541555" + random.NextInt(1000, 9999).ToString(CultureInfo.InvariantCulture);
        }

        if (random.NextBool(0.18d))
        {
            int position = random.NextInt(10);
            var builder = new StringBuilder(digits);
            builder[position] = (char)('0' + random.NextInt(10));
            digits = builder.ToString();
        }

        return random.NextInt(7) switch
        {
            0 => digits,
            1 => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
            2 => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}",
            3 => $"{digits[..3]}.{digits[3..6]}.{digits[6..]}",
            4 => $"+1 {digits[..3]} {digits[3..6]} {digits[6..]}",
            5 => $"1-{digits[..3]}-{digits[3..6]}-{digits[6..]}",
            _ => $"{digits[..3]}-{digits[3..6]}-{digits[6..]} x{random.NextInt(10, 999).ToString(CultureInfo.InvariantCulture)}",
        };
    }

    private static void MutateAddress(InsuranceLead lead, DeterministicRandom random)
    {
        lead.Address1 = lead.Address1
            .Replace(" STREET", random.NextBool() ? " ST" : " ST.", StringComparison.Ordinal)
            .Replace(" AVENUE", " AVE", StringComparison.Ordinal)
            .Replace(" ROAD", " RD", StringComparison.Ordinal)
            .Replace(" BOULEVARD", " BLVD", StringComparison.Ordinal)
            .Replace(" HIGHWAY", " HWY", StringComparison.Ordinal)
            .Replace(" NORTH", " N", StringComparison.Ordinal)
            .Replace(" SOUTH", " S", StringComparison.Ordinal)
            .Replace(" EAST", " E", StringComparison.Ordinal)
            .Replace(" WEST", " W", StringComparison.Ordinal);

        if (random.NextBool(0.45d))
        {
            lead.Address1 = $"{lead.Address1} {(random.NextBool() ? "APT" : "UNIT")} {random.NextInt(1, 40).ToString(CultureInfo.InvariantCulture)}";
            lead.Address2 = random.NextBool() ? $"#{random.NextInt(1, 40).ToString(CultureInfo.InvariantCulture)}" : string.Empty;
        }
        else if (!string.IsNullOrEmpty(lead.ZipCode) && random.NextBool(0.35d))
        {
            lead.ZipCode = $"{lead.ZipCode}-{random.NextInt(1000, 9999).ToString(CultureInfo.InvariantCulture)}";
        }
        else
        {
            lead.Address1 = lead.Address1.Replace(" ", "  ", StringComparison.Ordinal);
        }
    }

    private void MutateName(InsuranceLead lead, DeterministicRandom random)
    {
        int sample = random.NextInt(7);
        switch (sample)
        {
            case 0:
                lead.FirstName = _nameCatalog.CreateNickname(lead.FirstName, random);
                break;
            case 1:
                lead.MiddleInitial = string.IsNullOrEmpty(lead.MiddleInitial) ? ((char)('A' + random.NextInt(26))).ToString() : string.Empty;
                break;
            case 2:
                lead.NameSuffix = string.IsNullOrEmpty(lead.NameSuffix) ? "Jr" : string.Empty;
                break;
            case 3:
                lead.LastName = lead.LastName.Replace("'", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
                break;
            case 4:
                lead.FirstName = AddTypo(lead.FirstName, random);
                break;
            case 5:
                lead.FirstName = lead.FirstName.ToUpperInvariant();
                lead.LastName = lead.LastName.ToLowerInvariant();
                break;
            default:
                lead.LastName = lead.LastName.Contains('-', StringComparison.Ordinal) ? lead.LastName.Replace("-", string.Empty, StringComparison.Ordinal) : lead.LastName.Replace(" ", "-", StringComparison.Ordinal);
                break;
        }
    }

    private static void MutateFormatting(InsuranceLead lead, DeterministicRandom random)
    {
        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            lead.Email = random.NextBool() ? $" {lead.Email.Trim()}" : lead.Email.Trim().Replace("@", " @ ", StringComparison.Ordinal);
        }

        if (!string.IsNullOrWhiteSpace(lead.Phone))
        {
            lead.Phone = MutatePhone(lead.Phone, random);
            if (random.NextBool())
            {
                lead.Phone = $" {lead.Phone} ";
            }
        }

        lead.Address1 = random.NextBool() ? lead.Address1.ToUpperInvariant() : lead.Address1.Replace("  ", " ", StringComparison.Ordinal);
    }

    private void EnsureVariation(InsuranceLead original, InsuranceLead duplicate, DuplicateType duplicateType, DeterministicRandom random)
    {
        switch (duplicateType)
        {
            case DuplicateType.EMAIL_CHANGED when duplicate.Email == original.Email:
                duplicate.Email = string.IsNullOrWhiteSpace(original.Email)
                    ? _nameCatalog.CreateEmail(original.FirstName, original.LastName, random)
                    : $" {original.Email}";
                break;
            case DuplicateType.PHONE_CHANGED when duplicate.Phone == original.Phone:
                duplicate.Phone = string.IsNullOrWhiteSpace(original.Phone) ? "541-555-0199" : $"{new string(original.Phone.Where(char.IsDigit).ToArray())} x{random.NextInt(10, 999)}";
                break;
            case DuplicateType.ADDRESS_CHANGED when duplicate.Address1 == original.Address1 && duplicate.Address2 == original.Address2 && duplicate.ZipCode == original.ZipCode:
                duplicate.Address1 = $"{original.Address1} UNIT {random.NextInt(1, 20)}";
                break;
            case DuplicateType.NAME_VARIATION when duplicate.FirstName == original.FirstName && duplicate.MiddleInitial == original.MiddleInitial && duplicate.LastName == original.LastName && duplicate.NameSuffix == original.NameSuffix:
                duplicate.FirstName = _nameCatalog.CreateNickname(original.FirstName, random);
                if (duplicate.FirstName == original.FirstName)
                {
                    duplicate.LastName = $"{original.LastName}-Lee";
                }
                break;
            case DuplicateType.FORMATTING_VARIATION when duplicate.Email == original.Email && duplicate.Phone == original.Phone && duplicate.Address1 == original.Address1:
                duplicate.Address1 = $" {original.Address1} ";
                break;
            case DuplicateType.MULTIPLE_FIELDS_CHANGED when CountChangedGroups(original, duplicate) < 2:
                duplicate.Email = $" {MutateEmail(original, random).Trim()}";
                duplicate.Phone = MutatePhone(original.Phone, random);
                break;
        }

    }

    private static string AddTypo(string value, DeterministicRandom random)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            return value;
        }

        int index = random.NextInt(value.Length);
        char replacement = (char)('a' + random.NextInt(26));
        return string.Create(value.Length, (value, index, replacement), static (span, state) =>
        {
            state.value.AsSpan().CopyTo(span);
            span[state.index] = state.replacement;
        });
    }

    private static string BumpTimestamp(string value, DeterministicRandom random)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsed))
        {
            return value;
        }

        DateTime updated = parsed.AddMinutes(random.NextInt(5, 72 * 60));
        return updated.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    private static int CountChangedGroups(InsuranceLead original, InsuranceLead candidate)
    {
        int count = 0;
        if (candidate.FirstName != original.FirstName || candidate.MiddleInitial != original.MiddleInitial || candidate.LastName != original.LastName || candidate.NameSuffix != original.NameSuffix)
        {
            count++;
        }

        if (candidate.Email != original.Email)
        {
            count++;
        }

        if (candidate.Phone != original.Phone)
        {
            count++;
        }

        if (candidate.Address1 != original.Address1 || candidate.Address2 != original.Address2 || candidate.ZipCode != original.ZipCode)
        {
            count++;
        }

        return count;
    }
}
