namespace Lexql.Core.Relational.Sql;

public sealed record SqlToken(int Type, string Symbol, string Text, int Start, int Stop, int Channel);
