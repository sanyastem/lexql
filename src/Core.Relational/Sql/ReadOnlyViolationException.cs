namespace Lexql.Core.Relational.Sql;

public sealed class ReadOnlyViolationException : Exception
{
    public ReadOnlyViolationException()
        : base("This connection is read-only; write statements (INSERT/UPDATE/DELETE/DDL) are blocked.")
    {
    }
}
