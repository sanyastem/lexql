using Lexql.Core.Abstractions;

namespace Lexql.Core.Relational.Dml;

public sealed record DmlCommand(string Sql, IReadOnlyList<QueryParameter> Parameters);
