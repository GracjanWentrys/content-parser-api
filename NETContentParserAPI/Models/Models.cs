using System.Text.Json.Serialization;

namespace Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentType
{
    CSV,
    INTERNAL_JSON
}

public record ParseRequest(ContentType Type, string Content);

public record CsvRow(IReadOnlyDictionary<string, string> Columns);


public interface IParseResult
{
    bool IsSuccess { get; }
    int Count { get; }
    object? RawData { get; }
    string? ErrorMessage { get; }
}


public record ParseResult<T>(
    bool IsSuccess,
    int Count,
    T? Data,
    string? ErrorMessage = null
) : IParseResult
{
    public object? RawData => Data;

    public static ParseResult<T> Success(T data, int count) => new(true, count, data);
    public static ParseResult<T> Failure(string errorMessage) => new(false, 0, default, errorMessage);
}


public record ParseResponse<T>(
    bool IsSuccess,
    int RecordCount,
    T? Data,
    string? ErrorMessage = null
)
{
    public static ParseResponse<T> Success(T data, int count) => new(true, count, data, null);
    public static ParseResponse<T> Failure(string errorMessage) => new(false, 0, default, errorMessage);
}