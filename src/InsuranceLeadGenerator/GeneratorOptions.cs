namespace InsuranceData;

internal sealed class GeneratorOptions
{
    public const int DefaultSeed = 8675309;
    public const long DefaultRecordCount = 50_000_000;
    public const int DefaultFileCount = 10;
    public const double DefaultExactDuplicatePercent = 10.0;
    public const double DefaultModifiedDuplicatePercent = 16.0;
    public const double DefaultInvalidPercent = 2.0;
    public const int DefaultProgressInterval = 100_000;
    public const int DefaultFileBufferSize = 1_048_576;
    public const int DefaultReservoirSize = 200_000;

    public required string OutputDirectory { get; init; }
    public long TotalRecords { get; init; } = DefaultRecordCount;
    public int FileCount { get; init; } = DefaultFileCount;
    public int Seed { get; init; } = DefaultSeed;
    public double ExactDuplicatePercent { get; init; } = DefaultExactDuplicatePercent;
    public double ModifiedDuplicatePercent { get; init; } = DefaultModifiedDuplicatePercent;
    public double InvalidPercent { get; init; } = DefaultInvalidPercent;
    public bool ValidationMode { get; init; }
    public bool IncludeStructuralErrors { get; init; }
    public int ProgressInterval { get; init; } = DefaultProgressInterval;
    public int FileBufferSize { get; init; } = DefaultFileBufferSize;
    public int ReservoirSize { get; init; } = DefaultReservoirSize;
    public DateTime CurrentUtc { get; init; }

    public double UniquePercent => 100d - ExactDuplicatePercent - ModifiedDuplicatePercent - InvalidPercent;

    public long GetExactDuplicateTarget() => (long)Math.Round(TotalRecords * (ExactDuplicatePercent / 100d), MidpointRounding.AwayFromZero);

    public long GetModifiedDuplicateTarget() => (long)Math.Round(TotalRecords * (ModifiedDuplicatePercent / 100d), MidpointRounding.AwayFromZero);

    public long GetInvalidTarget() => (long)Math.Round(TotalRecords * (InvalidPercent / 100d), MidpointRounding.AwayFromZero);

    public long GetUniqueTarget()
    {
        long uniqueTarget = TotalRecords - GetExactDuplicateTarget() - GetModifiedDuplicateTarget() - GetInvalidTarget();
        if (uniqueTarget <= 0)
        {
            throw new ArgumentException("The configured duplicate percentages leave no room for unique records.");
        }

        return uniqueTarget;
    }

    public IReadOnlyList<long> GetFileRecordCounts()
    {
        long baseCount = TotalRecords / FileCount;
        long remainder = TotalRecords % FileCount;
        var counts = new long[FileCount];
        for (int i = 0; i < FileCount; i++)
        {
            counts[i] = baseCount + (i < remainder ? 1 : 0);
        }

        return counts;
    }
}
