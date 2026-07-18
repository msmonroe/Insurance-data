using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace InsuranceData;

internal static class ManifestWriter
{
    public static async Task WriteAsync(string outputDirectory, IReadOnlyList<FileGenerationStatistics> fileStatistics, CancellationToken cancellationToken)
    {
        string manifestPath = Path.Combine(outputDirectory, "insurance_leads_manifest.csv");
        using var stream = new FileStream(manifestPath, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        await writer.WriteLineAsync("FileName,RowCount,UniqueRecordCount,ExactDuplicateCount,ModifiedDuplicateCount,InvalidRecordCount,FileSizeBytes,SHA256Checksum,GenerationStartedUtc,GenerationCompletedUtc").ConfigureAwait(false);
        foreach (FileGenerationStatistics file in fileStatistics)
        {
            string line = string.Join(',',
                file.FileName,
                file.RowCount.ToString(CultureInfo.InvariantCulture),
                file.UniqueRecordCount.ToString(CultureInfo.InvariantCulture),
                file.ExactDuplicateCount.ToString(CultureInfo.InvariantCulture),
                file.ModifiedDuplicateCount.ToString(CultureInfo.InvariantCulture),
                file.InvalidRecordCount.ToString(CultureInfo.InvariantCulture),
                file.FileSizeBytes.ToString(CultureInfo.InvariantCulture),
                file.Sha256Checksum,
                file.GenerationStartedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                file.GenerationCompletedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    public static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }
}
