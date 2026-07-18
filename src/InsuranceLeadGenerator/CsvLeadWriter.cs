using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace InsuranceData;

internal sealed class CsvLeadWriter : IAsyncDisposable
{
    private readonly FileStream _stream;
    private readonly StreamWriter _writer;
    private readonly StringBuilder _rowBuilder = new(1024);
    private readonly bool _includeStructuralErrors;

    public CsvLeadWriter(string path, int bufferSize, bool includeStructuralErrors)
    {
        FilePath = path;
        _includeStructuralErrors = includeStructuralErrors;
        _stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        _writer = new StreamWriter(_stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize);
    }

    public string FilePath { get; }

    public long BytesWritten => _stream.Position;

    public async Task WriteHeaderAsync(CancellationToken cancellationToken)
    {
        await _writer.WriteLineAsync(string.Join(',', InsuranceLead.HeaderColumns)).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Security", "cs/exposure-of-sensitive-information", Justification = "This generator writes only fictional synthetic test data to caller-selected CSV files by design.")]
    public async Task WriteRecordAsync(InsuranceLead lead, CancellationToken cancellationToken)
    {
        _rowBuilder.Clear();

        AppendField(lead.LeadId.ToString(CultureInfo.InvariantCulture));
        AppendField(lead.SourceLeadId);
        AppendField(lead.FirstName);
        AppendField(lead.MiddleInitial);
        AppendField(lead.LastName);
        AppendField(lead.NameSuffix);
        AppendField(lead.DateOfBirth);
        AppendField(lead.Gender);
        AppendField(lead.Email);
        AppendField(lead.Phone);
        AppendField(lead.Address1);
        AppendField(lead.Address2);
        AppendField(lead.City);
        AppendField(lead.County);
        AppendField(lead.State);
        AppendField(lead.ZipCode);
        AppendField(lead.Occupation);
        AppendField(lead.MaritalStatus);
        AppendField(lead.AnnualIncome);
        AppendField(lead.HomeOwner);
        AppendField(lead.TobaccoUser);
        AppendField(lead.InsuranceType);
        AppendField(lead.LeadSource);
        AppendField(lead.CampaignCode);
        AppendField(lead.VendorCode);
        AppendField(lead.ConsentToContact);
        AppendField(lead.CreatedDate);
        AppendField(lead.ModifiedDate);
        AppendField(lead.DuplicateType.ToString());

        if (_includeStructuralErrors && lead.DuplicateType == DuplicateType.INTENTIONALLY_INVALID && lead.CampaignCode == "BROKEN,FIELD,COUNT")
        {
            _rowBuilder.Append(lead.OriginalLeadId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            _rowBuilder.Append(",UNBALANCED\"");
        }
        else
        {
            AppendField(lead.OriginalLeadId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, finalField: true);
        }

        await _writer.WriteLineAsync(_rowBuilder.ToString()).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
        {
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken) => _writer.FlushAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false);
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    private void AppendField(string value, bool finalField = false)
    {
        CsvEscaper.AppendEscaped(_rowBuilder, value);
        if (!finalField)
        {
            _rowBuilder.Append(',');
        }
    }
}
