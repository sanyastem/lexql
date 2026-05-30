namespace Lexql.Core.Relational.Tree;

public interface IRelationalCatalog
{
    Task<IReadOnlyList<string>> ListNamespacesAsync(CancellationToken ct);
}
