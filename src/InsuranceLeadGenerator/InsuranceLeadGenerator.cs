using System.Diagnostics;
using System.Globalization;

namespace InsuranceData;

internal sealed class InsuranceLeadGenerator
{
    private static readonly DateTime CreatedDateFloorUtc = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly GeneratorOptions _options;
    private readonly LocationCatalog _locationCatalog = new();
    private readonly NameCatalog _nameCatalog = new();
    private readonly DuplicateGenerator _duplicateGenerator;
    private readonly DirtyDataGenerator _dirtyDataGenerator = new();
    private readonly ValidationInspector _validationInspector;

    private readonly string[] _streetNames =
    {
        "Maple", "Oak", "Cedar", "Pine", "Willow", "Elm", "Juniper", "Aspen", "Heritage", "Summit",
        "Liberty", "Harbor", "Meadow", "River", "Brook", "Copper", "Silver", "Redwood", "Prairie", "Lakeside",
    };

    private readonly string[] _streetSuffixes = { "STREET", "AVENUE", "ROAD", "BOULEVARD", "HIGHWAY", "LANE", "DRIVE" };
    private readonly string[] _streetDirections = { "", "", "NORTH", "SOUTH", "EAST", "WEST" };
    private readonly string[] _vendorCodes = { "VEND-A01", "VEND-B14", "VEND-C22", "VEND-D37", "VEND-E52", "VEND-F63" };
    private readonly string[] _campaignRoots = { "AUTO", "HOME", "LIFE", "HEALTH", "RET", "MED", "SMB", "UMB" };

    public InsuranceLeadGenerator(GeneratorOptions options)
    {
        _options = options;
        _duplicateGenerator = new DuplicateGenerator(_nameCatalog);
        _validationInspector = new ValidationInspector(options.ValidationMode);
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.OutputDirectory);

        long uniqueRemaining = _options.GetUniqueTarget();
        long exactRemaining = _options.GetExactDuplicateTarget();
        long modifiedRemaining = _options.GetModifiedDuplicateTarget();
        long invalidRemaining = _options.GetInvalidTarget();
        IReadOnlyList<long> fileRecordCounts = _options.GetFileRecordCounts();

        var statistics = new GenerationStatistics();
        var duplicateSources = new List<DuplicateSource>(_options.ReservoirSize);
        int duplicateSourceInsertIndex = 0;
        long nextLeadId = 0;
        long nextLogicalRecordNumber = 0;
        var stopwatch = Stopwatch.StartNew();
        bool cancelled = false;

        for (int fileIndex = 0; fileIndex < fileRecordCounts.Count; fileIndex++)
        {
            string fileName = $"insurance_leads_{fileIndex + 1:000}.csv";
            string filePath = Path.Combine(_options.OutputDirectory, fileName);
            DateTime fileStartedUtc = DateTime.UtcNow;
            var fileStatistics = new FileGenerationStatistics
            {
                FileName = fileName,
                GenerationStartedUtc = fileStartedUtc,
                Sha256Checksum = string.Empty,
            };

            try
            {
                await using var writer = new CsvLeadWriter(filePath, _options.FileBufferSize, _options.IncludeStructuralErrors);
                await writer.WriteHeaderAsync(cancellationToken).ConfigureAwait(false);

                long recordsForFile = fileRecordCounts[fileIndex];
                for (long indexInFile = 0; indexInFile < recordsForFile; indexInFile++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }

                    nextLeadId++;
                    RecordKind kind = SelectRecordKind(nextLeadId, uniqueRemaining, exactRemaining, modifiedRemaining, invalidRemaining, duplicateSources.Count);
                    InsuranceLead lead = kind switch
                    {
                        RecordKind.Unique => CreateUniqueLead(++nextLogicalRecordNumber, nextLeadId),
                        RecordKind.Invalid => CreateInvalidLead(++nextLogicalRecordNumber, nextLeadId),
                        RecordKind.ExactDuplicate => CreateExactDuplicate(nextLeadId, SelectDuplicateSource(duplicateSources, nextLeadId)),
                        RecordKind.ModifiedDuplicate => CreateModifiedDuplicate(nextLeadId, SelectDuplicateSource(duplicateSources, nextLeadId)),
                        _ => throw new InvalidOperationException("Unknown record kind."),
                    };

                    switch (kind)
                    {
                        case RecordKind.Unique:
                            uniqueRemaining--;
                            AddDuplicateSource(duplicateSources, ref duplicateSourceInsertIndex, new DuplicateSource(lead.LeadId, nextLogicalRecordNumber));
                            fileStatistics.UniqueRecordCount++;
                            break;
                        case RecordKind.Invalid:
                            invalidRemaining--;
                            fileStatistics.InvalidRecordCount++;
                            break;
                        case RecordKind.ExactDuplicate:
                            exactRemaining--;
                            fileStatistics.ExactDuplicateCount++;
                            break;
                        case RecordKind.ModifiedDuplicate:
                            modifiedRemaining--;
                            fileStatistics.ModifiedDuplicateCount++;
                            break;
                    }

                    await writer.WriteRecordAsync(lead, cancellationToken).ConfigureAwait(false);
                    fileStatistics.RowCount++;
                    statistics.Record(lead, statistics.ApproximateBytesWritten + writer.BytesWritten);
                    _validationInspector.Track(lead);

                    if (_options.ProgressInterval > 0 && nextLeadId % _options.ProgressInterval == 0)
                    {
                        ReportProgress(fileIndex + 1, writer.BytesWritten, nextLeadId, stopwatch.Elapsed);
                    }
                }

                await writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                fileStatistics.FileSizeBytes = new FileInfo(filePath).Length;
            }
            finally
            {
                fileStatistics.GenerationCompletedUtc = DateTime.UtcNow;
            }

            await ValidationInspector.ValidateFileMetadataAsync(filePath, InsuranceLead.HeaderColumns, cancellationToken).ConfigureAwait(false);
            fileStatistics.Sha256Checksum = await ManifestWriter.ComputeSha256Async(filePath, cancellationToken).ConfigureAwait(false);
            statistics.Files.Add(fileStatistics);

            if (cancelled)
            {
                break;
            }
        }

        await ManifestWriter.WriteAsync(_options.OutputDirectory, statistics.Files, CancellationToken.None).ConfigureAwait(false);
        await _validationInspector.ValidateFilesAsync(_options.OutputDirectory, statistics.Files, _options, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        Console.WriteLine($"Completed {statistics.TotalRowsWritten.ToString("N0", CultureInfo.InvariantCulture)} record(s) in {stopwatch.Elapsed}.");
        Console.WriteLine($"Manifest written to {Path.Combine(_options.OutputDirectory, "insurance_leads_manifest.csv")}");

        return cancelled ? 2 : 0;
    }

    private InsuranceLead CreateUniqueLead(long logicalRecordNumber, long leadId)
    {
        InsuranceLead lead = GenerateBaseRecord(logicalRecordNumber);
        lead.LeadId = leadId;
        lead.DuplicateType = DuplicateType.UNIQUE;
        lead.OriginalLeadId = null;
        return lead;
    }

    private InsuranceLead CreateInvalidLead(long logicalRecordNumber, long leadId)
    {
        InsuranceLead lead = GenerateBaseRecord(logicalRecordNumber);
        lead.LeadId = leadId;
        _dirtyDataGenerator.Apply(lead, _options.Seed, _options.IncludeStructuralErrors);
        return lead;
    }

    private InsuranceLead CreateExactDuplicate(long newLeadId, DuplicateSource duplicateSource)
    {
        InsuranceLead original = CreateUniqueLead(duplicateSource.LogicalRecordNumber, duplicateSource.OriginalLeadId);
        return _duplicateGenerator.CreateExactDuplicate(original, newLeadId, duplicateSource.OriginalLeadId);
    }

    private InsuranceLead CreateModifiedDuplicate(long newLeadId, DuplicateSource duplicateSource)
    {
        InsuranceLead original = CreateUniqueLead(duplicateSource.LogicalRecordNumber, duplicateSource.OriginalLeadId);
        return _duplicateGenerator.CreateModifiedDuplicate(original, newLeadId, duplicateSource.OriginalLeadId, _options.Seed);
    }

    public InsuranceLead GenerateBaseRecord(long logicalRecordNumber)
    {
        var random = new DeterministicRandom(_options.Seed, logicalRecordNumber);
        LocationEntry location = _locationCatalog.PickLocation(random);
        NameParts name = _nameCatalog.CreateName(random);
        InsuranceType insuranceType = PickInsuranceType(random);
        LeadSource leadSource = PickLeadSource(random);
        string vendorCode = random.NextItem(_vendorCodes);
        string campaignCode = $"{random.NextItem(_campaignRoots)}-{location.StateCode}-{random.NextInt(100, 999).ToString(CultureInfo.InvariantCulture)}";
        string address1 = BuildAddress1(random);
        string address2 = random.NextBool(0.26d) ? $"APT {random.NextInt(1, 40).ToString(CultureInfo.InvariantCulture)}" : string.Empty;
        string zipCode = random.NextBool(0.01d) ? string.Empty : location.ZipCode;
        string email = random.NextBool(0.05d) ? string.Empty : _nameCatalog.CreateEmail(name.FirstName, name.LastName, random);
        string phone = random.NextBool(0.03d) ? string.Empty : BuildPhone(location, random);
        string dateOfBirth = random.NextBool(0.01d) ? string.Empty : BuildDateOfBirth(random);
        if (!string.IsNullOrEmpty(address2) && random.NextBool(0.02d))
        {
            address2 = string.Empty;
        }

        if (random.NextBool(0.005d))
        {
            name = name with { LastName = string.Empty };
        }

        DateTime createdDate = BuildCreatedDate(random);
        DateTime modifiedDate = createdDate.AddDays(random.NextInt(0, 365)).AddMinutes(random.NextInt(0, 24 * 60));
        if (modifiedDate > _options.CurrentUtc)
        {
            modifiedDate = _options.CurrentUtc;
        }

        return new InsuranceLead
        {
            SourceLeadId = $"{vendorCode}-{logicalRecordNumber:0000000000}",
            FirstName = name.FirstName,
            MiddleInitial = name.MiddleInitial,
            LastName = name.LastName,
            NameSuffix = name.Suffix,
            DateOfBirth = dateOfBirth,
            Gender = name.Gender,
            Email = email,
            Phone = phone,
            Address1 = address1,
            Address2 = address2,
            City = location.City,
            County = location.County,
            State = location.StateCode,
            ZipCode = zipCode,
            Occupation = _nameCatalog.CreateOccupation(random),
            MaritalStatus = PickMaritalStatus(random),
            AnnualIncome = BuildIncome(random, insuranceType),
            HomeOwner = DetermineHomeOwner(random, insuranceType),
            TobaccoUser = random.NextBool(0.18d) ? "TRUE" : "FALSE",
            InsuranceType = FormatInsuranceType(insuranceType),
            LeadSource = leadSource.ToString(),
            CampaignCode = campaignCode,
            VendorCode = vendorCode,
            ConsentToContact = random.NextBool(0.91d) ? "TRUE" : "FALSE",
            CreatedDate = createdDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ModifiedDate = modifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
        };
    }

    private RecordKind SelectRecordKind(long leadId, long uniqueRemaining, long exactRemaining, long modifiedRemaining, long invalidRemaining, int duplicateSourceCount)
    {
        if (duplicateSourceCount == 0)
        {
            return uniqueRemaining > 0 ? RecordKind.Unique : RecordKind.Invalid;
        }

        long totalRemaining = uniqueRemaining + exactRemaining + modifiedRemaining + invalidRemaining;
        if (totalRemaining <= 0)
        {
            throw new InvalidOperationException("No records remain to be generated.");
        }

        var random = new DeterministicRandom(_options.Seed ^ 0x55AA55AA, leadId);
        long sample = random.NextLong(totalRemaining);
        if (sample < uniqueRemaining)
        {
            return RecordKind.Unique;
        }

        sample -= uniqueRemaining;
        if (sample < exactRemaining)
        {
            return RecordKind.ExactDuplicate;
        }

        sample -= exactRemaining;
        if (sample < modifiedRemaining)
        {
            return RecordKind.ModifiedDuplicate;
        }

        if (invalidRemaining > 0)
        {
            return RecordKind.Invalid;
        }

        return uniqueRemaining > 0 ? RecordKind.Unique : RecordKind.ModifiedDuplicate;
    }

    private static void AddDuplicateSource(List<DuplicateSource> sources, ref int insertIndex, DuplicateSource source)
    {
        if (sources.Count < sources.Capacity)
        {
            sources.Add(source);
            return;
        }

        sources[insertIndex] = source;
        insertIndex++;
        if (insertIndex >= sources.Count)
        {
            insertIndex = 0;
        }
    }

    private DuplicateSource SelectDuplicateSource(IReadOnlyList<DuplicateSource> duplicateSources, long leadId)
    {
        if (duplicateSources.Count == 0)
        {
            throw new InvalidOperationException("A duplicate was requested before any duplicate source existed.");
        }

        var random = new DeterministicRandom(_options.Seed ^ 0x0F0F0F0F, leadId);
        return duplicateSources[random.NextInt(duplicateSources.Count)];
    }

    private void ReportProgress(int currentFileNumber, long currentFileBytes, long totalRecordsWritten, TimeSpan elapsed)
    {
        double rowsPerSecond = elapsed.TotalSeconds <= 0 ? 0d : totalRecordsWritten / elapsed.TotalSeconds;
        long approximateBytes = statisticsCache(currentFileBytes);
        Console.WriteLine(
            $"[{DateTime.UtcNow:O}] File {currentFileNumber}/{_options.FileCount} | Records {totalRecordsWritten.ToString("N0", CultureInfo.InvariantCulture)} | Rows/sec {rowsPerSecond.ToString("N0", CultureInfo.InvariantCulture)} | Elapsed {elapsed:hh\\:mm\\:ss} | Approx Size {FormatBytes(approximateBytes)}");

        long statisticsCache(long bytesForCurrentFile)
        {
            long completedFileBytes = 0;
            return completedFileBytes + bytesForCurrentFile;
        }
    }

    private string BuildAddress1(DeterministicRandom random)
    {
        int streetNumber = random.NextInt(101, 9899);
        string direction = random.NextItem(_streetDirections);
        string streetName = random.NextItem(_streetNames);
        string suffix = random.NextItem(_streetSuffixes);
        return string.IsNullOrEmpty(direction)
            ? $"{streetNumber} {streetName} {suffix}"
            : $"{streetNumber} {direction} {streetName} {suffix}";
    }

    private static string BuildPhone(LocationEntry location, DeterministicRandom random)
    {
        string areaCode = random.NextItem(location.AreaCodes);
        string suffix = random.NextInt(1000, 9999).ToString(CultureInfo.InvariantCulture);
        return random.NextInt(6) switch
        {
            0 => $"{areaCode}555{suffix}",
            1 => $"{areaCode}-555-{suffix}",
            2 => $"({areaCode}) 555-{suffix}",
            3 => $"{areaCode}.555.{suffix}",
            4 => $"+1 {areaCode} 555 {suffix}",
            _ => $"1-{areaCode}-555-{suffix}",
        };
    }

    private string BuildDateOfBirth(DeterministicRandom random)
    {
        int age = random.NextInt(18, 91);
        DateTime latestBirthday = _options.CurrentUtc.Date.AddYears(-age);
        DateTime earliestBirthday = latestBirthday.AddYears(-1).AddDays(1);
        int range = Math.Max(1, (latestBirthday - earliestBirthday).Days + 1);
        DateTime selected = earliestBirthday.AddDays(random.NextInt(range));
        return selected.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private DateTime BuildCreatedDate(DeterministicRandom random)
    {
        TimeSpan span = _options.CurrentUtc - CreatedDateFloorUtc;
        long totalMinutes = Math.Max(1L, (long)span.TotalMinutes);
        return CreatedDateFloorUtc.AddMinutes(random.NextLong(totalMinutes));
    }

    private static string BuildIncome(DeterministicRandom random, InsuranceType insuranceType)
    {
        int baseIncome = insuranceType switch
        {
            InsuranceType.CommercialAuto or InsuranceType.SmallBusiness => random.NextInt(60_000, 220_000),
            InsuranceType.Medicare => random.NextInt(28_000, 110_000),
            _ => random.NextInt(35_000, 185_000),
        };

        return baseIncome.ToString(CultureInfo.InvariantCulture);
    }

    private static string DetermineHomeOwner(DeterministicRandom random, InsuranceType insuranceType)
    {
        return insuranceType switch
        {
            InsuranceType.Home or InsuranceType.Umbrella or InsuranceType.SmallBusiness => "TRUE",
            InsuranceType.Renters => "FALSE",
            _ => random.NextBool(0.58d) ? "TRUE" : "FALSE",
        };
    }

    private static InsuranceType PickInsuranceType(DeterministicRandom random)
    {
        int sample = random.NextInt(100);
        return sample switch
        {
            < 22 => InsuranceType.Auto,
            < 36 => InsuranceType.Home,
            < 45 => InsuranceType.Renters,
            < 57 => InsuranceType.Life,
            < 69 => InsuranceType.Health,
            < 77 => InsuranceType.Medicare,
            < 84 => InsuranceType.Supplemental,
            < 90 => InsuranceType.CommercialAuto,
            < 96 => InsuranceType.SmallBusiness,
            _ => InsuranceType.Umbrella,
        };
    }

    private static LeadSource PickLeadSource(DeterministicRandom random)
    {
        int sample = random.NextInt(100);
        return sample switch
        {
            < 26 => LeadSource.WEB_FORM,
            < 40 => LeadSource.PARTNER,
            < 49 => LeadSource.CALL_CENTER,
            < 58 => LeadSource.PURCHASED_LIST,
            < 66 => LeadSource.REFERRAL,
            < 76 => LeadSource.LANDING_PAGE,
            < 84 => LeadSource.SOCIAL_MEDIA,
            < 90 => LeadSource.DIRECT_MAIL,
            < 95 => LeadSource.EVENT,
            _ => LeadSource.INTERNAL,
        };
    }

    private static string PickMaritalStatus(DeterministicRandom random)
    {
        int sample = random.NextInt(100);
        MaritalStatus status = sample switch
        {
            < 32 => MaritalStatus.Single,
            < 69 => MaritalStatus.Married,
            < 82 => MaritalStatus.Divorced,
            < 91 => MaritalStatus.DomesticPartner,
            _ => MaritalStatus.Widowed,
        };

        return status switch
        {
            MaritalStatus.DomesticPartner => "Domestic Partner",
            _ => status.ToString(),
        };
    }

    private static string FormatInsuranceType(InsuranceType insuranceType)
    {
        return insuranceType switch
        {
            InsuranceType.CommercialAuto => "Commercial Auto",
            InsuranceType.SmallBusiness => "Small Business",
            _ => insuranceType.ToString(),
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int index = 0;
        while (value >= 1024 && index < suffixes.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return $"{value:0.##} {suffixes[index]}";
    }

    private enum RecordKind
    {
        Unique,
        ExactDuplicate,
        ModifiedDuplicate,
        Invalid,
    }

    private readonly record struct DuplicateSource(long OriginalLeadId, long LogicalRecordNumber);
}
