using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using Api.Models;

namespace Api.Services;

public interface IContentDecoder
{
    string DecodeBase64(string encodedContent);
}

public class Base64ContentDecoder : IContentDecoder
{
    public string DecodeBase64(string encodedContent)
    {
        if (string.IsNullOrWhiteSpace(encodedContent))
        {
            throw new ArgumentException("Payload content cannot be empty.");
        }
        Span<byte> buffer = new Span<byte>(new byte[encodedContent.Length]);

        if (!Convert.TryFromBase64String(encodedContent, buffer, out int bytesWritten))
        {
            throw new ArgumentException("The provided content is not a valid Base-64 string.");
        }

        return Encoding.UTF8.GetString(buffer.Slice(0, bytesWritten));
    }
}

public interface IContentParser
{
    ContentType SupportedType { get; }
    IParseResult Parse(string rawContent);
}

public class CsvContentParser : IContentParser
{
    public ContentType SupportedType => ContentType.CSV;

    public IParseResult Parse(string rawContent)
    {
        var trimmed = rawContent.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ParseResult<List<CsvRow>>.Success(new List<CsvRow>(), 0);
        }

        using var stringReader = new StringReader(trimmed);
        using var csvReader = new TextFieldParser(stringReader);

        csvReader.SetDelimiters(",");
        csvReader.HasFieldsEnclosedInQuotes = true;

        string[]? headers = csvReader.ReadFields();
        if (headers == null || headers.Length == 0)
        {
            return ParseResult<List<CsvRow>>.Success(new List<CsvRow>(), 0);
        }

        var rows = new List<CsvRow>();

        while (!csvReader.EndOfData)
        {
            string[]? fields = csvReader.ReadFields();
            if (fields == null) continue;

            var columns = new Dictionary<string, string>();
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim();
                var value = i < fields.Length ? fields[i] : string.Empty;
                columns[header] = value;
            }

            rows.Add(new CsvRow(columns));
        }

        return ParseResult<List<CsvRow>>.Success(rows, rows.Count);
    }
}

public class InternalJsonContentParser : IContentParser
{
    public ContentType SupportedType => ContentType.INTERNAL_JSON;

    public IParseResult Parse(string rawContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawContent);
            var root = doc.RootElement.Clone();

            if (root.ValueKind == JsonValueKind.Array)
            {
                return ParseResult<JsonElement>.Success(root, root.GetArrayLength());
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                return ParseResult<JsonElement>.Success(root, 1);
            }

            return ParseResult<JsonElement>.Failure("Invalid JSON structure (expected array or object).");
        }
        catch (JsonException ex)
        {
            return ParseResult<JsonElement>.Failure($"JSON Parsing Error: {ex.Message}");
        }
    }
}

public class ContentParserFactory
{
    private readonly Dictionary<ContentType, IContentParser> _parsers;

    public ContentParserFactory(IEnumerable<IContentParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.SupportedType);
    }

    public IContentParser GetParser(ContentType type)
    {
        if (_parsers.TryGetValue(type, out var parser))
        {
            return parser;
        }

        throw new NotSupportedException($"Content type '{type}' is not supported.");
    }
}