using System.Globalization;
using System.Text;
using FloodAI.DataAccess.Observations;
using Microsoft.VisualBasic.FileIO;

namespace FloodAI.Business.Observations;

public static class VanUcCsvReader
{
    private static readonly string[] Header =
    [
        "timestamp_local",
        "water_level_trung_trang_raw",
        "water_level_hon_dau_raw",
        "discharge_trung_trang_raw"
    ];

    private static readonly DateTime Start = new(2021, 1, 1);
    private static readonly DateTime EndExclusive = new(2023, 1, 1);

    public static IReadOnlyList<VanUcHourlyRow> Read(string path, CancellationToken cancellationToken)
    {
        using var parser = new TextFieldParser(path, new UTF8Encoding(false, true));
        parser.SetDelimiters(",");

        if (parser.ReadFields() is not { } header || !header.SequenceEqual(Header))
        {
            throw new InvalidDataException("Unexpected Văn Úc CSV header.");
        }

        var rows = new List<VanUcHourlyRow>();
        var expected = Start;
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = parser.LineNumber;
            var fields = parser.ReadFields();
            if (fields is not { Length: 4 } ||
                !DateTime.TryParseExact(fields[0], "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp) ||
                timestamp != expected || timestamp >= EndExclusive)
            {
                throw new InvalidDataException(
                    $"Invalid row or missing, repeated, or out-of-period hour at CSV line {line}. Expected {expected:yyyy-MM-dd HH:mm:ss}.");
            }

            rows.Add(new VanUcHourlyRow(
                timestamp,
                ReadNumber(fields[1], line, Header[1]),
                ReadNumber(fields[2], line, Header[2]),
                ReadNumber(fields[3], line, Header[3])));
            expected = expected.AddHours(1);
        }

        if (expected != EndExclusive)
        {
            throw new InvalidDataException($"The CSV ended before the expected hour {expected:yyyy-MM-dd HH:mm:ss}.");
        }

        return rows;
    }

    private static double ReadNumber(string field, long line, string column)
    {
        if (!double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !double.IsFinite(value))
        {
            throw new InvalidDataException($"Invalid numeric value in {column} at CSV line {line}.");
        }

        return value;
    }
}
