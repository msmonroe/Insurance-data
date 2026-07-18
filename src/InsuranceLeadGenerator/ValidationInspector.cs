using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace InsuranceData;

internal sealed class ValidationInspector
{
    private readonly bool _enabled;
    private readonly Dictionary<long, InsuranceLead> _recordsByLeadId = new();
    private readonly HashSet<long> _leadIds = new();
    private readonly Dictionary<DuplicateType, long> _counts = new();

    public ValidationInspector(bool enabled)
    {
        _enabled = enabled;
    }

    public void Track(InsuranceLead lead)
    {
        if (!_enabled)
        {
            return;
        }

        if (!_leadIds.Add(lead.LeadId))
        {
            throw new InvalidOperationException($"Duplicate LeadId detected during validation tracking: {lead.LeadId}.");
        }

        _counts[lead.DuplicateType] = _counts.GetValueOrDefault(lead.DuplicateType) + 1;
        _recordsByLeadId[lead.LeadId] = lead.Clone();

        if (lead.DuplicateType == DuplicateType.UNIQUE || lead.DuplicateType == DuplicateType.INTENTIONALLY_INVALID)
        {
            if (lead.OriginalLeadId.HasValue)
            {
                throw new InvalidOperationException($"Lead {lead.LeadId} should not have an OriginalLeadId value.");
            }

            return;
        }

        if (!lead.OriginalLeadId.HasValue || !_recordsByLeadId.TryGetValue(lead.OriginalLeadId.Value, out InsuranceLead? original))
        {
            throw new InvalidOperationException($"Lead {lead.LeadId} references missing original lead {lead.OriginalLeadId}.");
        }

        ValidateDuplicateAgainstOriginal(original, lead);
    }

    public async Task ValidateFilesAsync(string outputDirectory, IReadOnlyList<FileGenerationStatistics> fileStatistics, GeneratorOptions options, CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            return;
        }

        foreach (FileGenerationStatistics file in fileStatistics)
        {
            string path = Path.Combine(outputDirectory, file.FileName);
            long rowCount = 0;
            using var parser = new TextFieldParser(path)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false,
            };
            parser.SetDelimiters(",");

            string[]? header = parser.ReadFields();
            if (header is null || !header.SequenceEqual(InsuranceLead.HeaderColumns))
            {
                throw new InvalidOperationException($"Header validation failed for {file.FileName}.");
            }

            while (!parser.EndOfData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[]? fields = parser.ReadFields();
                if (fields is null)
                {
                    continue;
                }

                if (fields.Length != InsuranceLead.HeaderColumns.Count)
                {
                    throw new InvalidOperationException($"Unexpected field count in {file.FileName}. Expected {InsuranceLead.HeaderColumns.Count}, found {fields.Length}.");
                }

                rowCount++;
            }

            if (rowCount != file.RowCount)
            {
                throw new InvalidOperationException($"Row count validation failed for {file.FileName}. Expected {file.RowCount}, found {rowCount}.");
            }
        }

        ValidatePercentage(options.ExactDuplicatePercent, Count(DuplicateType.EXACT), options.TotalRecords, "exact duplicates");
        ValidatePercentage(options.ModifiedDuplicatePercent, CountModifiedDuplicates(), options.TotalRecords, "modified duplicates");
        ValidatePercentage(options.InvalidPercent, Count(DuplicateType.INTENTIONALLY_INVALID), options.TotalRecords, "invalid rows");
    }

    public static async Task ValidateFileMetadataAsync(string path, IReadOnlyList<string> expectedHeader, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        string? headerLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (headerLine is null)
        {
            throw new InvalidOperationException($"The generated file '{path}' is empty.");
        }

        if (!string.Equals(headerLine, string.Join(',', expectedHeader), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The generated file '{path}' has an unexpected header.");
        }
    }

    private void ValidateDuplicateAgainstOriginal(InsuranceLead original, InsuranceLead candidate)
    {
        if (candidate.DuplicateType == DuplicateType.EXACT)
        {
            if (!candidate.BusinessFieldsEqual(original))
            {
                throw new InvalidOperationException($"Exact duplicate lead {candidate.LeadId} does not match original lead {original.LeadId}.");
            }

            return;
        }

        if (candidate.BusinessFieldsEqual(original))
        {
            throw new InvalidOperationException($"Modified duplicate lead {candidate.LeadId} did not change any business fields.");
        }

        bool valid = candidate.DuplicateType switch
        {
            DuplicateType.EMAIL_CHANGED => candidate.Email != original.Email,
            DuplicateType.PHONE_CHANGED => candidate.Phone != original.Phone,
            DuplicateType.ADDRESS_CHANGED => candidate.Address1 != original.Address1 || candidate.Address2 != original.Address2 || candidate.ZipCode != original.ZipCode,
            DuplicateType.NAME_VARIATION => candidate.FirstName != original.FirstName || candidate.MiddleInitial != original.MiddleInitial || candidate.LastName != original.LastName || candidate.NameSuffix != original.NameSuffix,
            DuplicateType.FORMATTING_VARIATION => candidate.Email != original.Email || candidate.Phone != original.Phone || candidate.Address1 != original.Address1,
            DuplicateType.MULTIPLE_FIELDS_CHANGED => CountChangedGroups(original, candidate) >= 2,
            _ => false,
        };

        if (!valid)
        {
            throw new InvalidOperationException($"Lead {candidate.LeadId} failed duplicate validation for {candidate.DuplicateType}.");
        }
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

    private static void ValidatePercentage(double expectedPercent, long observedCount, long totalCount, string label)
    {
        double observedPercent = totalCount == 0 ? 0d : observedCount * 100d / totalCount;
        if (Math.Abs(observedPercent - expectedPercent) > 1.5d)
        {
            throw new InvalidOperationException($"The observed percentage for {label} ({observedPercent:F2}%) was too far from the configured value of {expectedPercent:F2}%.");
        }
    }

    private long Count(DuplicateType type) => _counts.GetValueOrDefault(type);

    private long CountModifiedDuplicates() => _counts.Where(static pair => pair.Key is not DuplicateType.UNIQUE and not DuplicateType.EXACT and not DuplicateType.INTENTIONALLY_INVALID).Sum(static pair => pair.Value);
}
