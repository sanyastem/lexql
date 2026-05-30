namespace Lexql.Core.Relational.Schema;

public interface ISchemaCache
{
    void Invalidate(string @namespace);

    void InvalidateAll();
}
