using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Security",
    "cs/exposure-of-sensitive-information",
    Justification = "The application intentionally writes only fictional synthetic lead data to CSV files for import testing.",
    Scope = "member",
    Target = "~M:InsuranceData.CsvLeadWriter.WriteRecordAsync(InsuranceData.InsuranceLead,System.Threading.CancellationToken)~System.Threading.Tasks.Task")]
