using System.Text;

namespace InsuranceData;

internal static class CsvEscaper
{
    public static void AppendEscaped(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        bool mustQuote = value.IndexOfAny([',', '"', '\r', '\n']) >= 0;
        if (!mustQuote)
        {
            builder.Append(value);
            return;
        }

        builder.Append('"');
        foreach (char character in value)
        {
            if (character == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(character);
            }
        }

        builder.Append('"');
    }
}
