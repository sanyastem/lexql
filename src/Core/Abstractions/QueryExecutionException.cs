namespace Lexql.Core.Abstractions;

public sealed class QueryExecutionException : Exception
{
    public QueryExecutionException(string message, int? code, string? sqlState, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
        SqlState = sqlState;
    }

    public int? Code { get; }

    public string? SqlState { get; }
}
