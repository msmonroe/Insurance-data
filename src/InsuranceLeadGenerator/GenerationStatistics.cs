namespace InsuranceData;

internal sealed class GenerationStatistics
{
    public DateTime GenerationStartedUtc { get; } = DateTime.UtcNow;
    public long TotalRowsWritten { get; private set; }
    public long TotalUniqueRows { get; private set; }
    public long TotalExactDuplicates { get; private set; }
    public long TotalModifiedDuplicates { get; private set; }
    public long TotalInvalidRows { get; private set; }
    public long ApproximateBytesWritten { get; private set; }
    public List<FileGenerationStatistics> Files { get; } = new();

    public void Record(InsuranceLead lead, long bytesWritten)
    {
        TotalRowsWritten++;
        ApproximateBytesWritten = bytesWritten;
        switch (lead.DuplicateType)
        {
            case DuplicateType.UNIQUE:
                TotalUniqueRows++;
                break;
            case DuplicateType.EXACT:
                TotalExactDuplicates++;
                break;
            case DuplicateType.INTENTIONALLY_INVALID:
                TotalInvalidRows++;
                break;
            default:
                TotalModifiedDuplicates++;
                break;
        }
    }
}

internal sealed class FileGenerationStatistics
{
    public required string FileName { get; init; }
    public long RowCount { get; set; }
    public long UniqueRecordCount { get; set; }
    public long ExactDuplicateCount { get; set; }
    public long ModifiedDuplicateCount { get; set; }
    public long InvalidRecordCount { get; set; }
    public long FileSizeBytes { get; set; }
    public required string Sha256Checksum { get; set; }
    public required DateTime GenerationStartedUtc { get; init; }
    public DateTime GenerationCompletedUtc { get; set; }
}
