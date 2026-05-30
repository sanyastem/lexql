using Lexql.Core.Relational;
using Lexql.Core.Relational.Schema;
using NSubstitute;

namespace Lexql.Core.Tests.Metadata;

public class CachingSchemaReaderTests
{
    private static RelationalSchema Schema(string name) => new(name, [], [], []);

    [Fact]
    public async Task LoadAsync_CachesResultPerNamespace()
    {
        var inner = Substitute.For<IRelationalSchemaReader>();
        inner.LoadAsync("shop", Arg.Any<CancellationToken>()).Returns(Schema("shop"));
        var reader = new CachingSchemaReader(inner);

        var first = await reader.LoadAsync("shop", CancellationToken.None);
        var second = await reader.LoadAsync("shop", CancellationToken.None);

        Assert.Same(first, second);
        await inner.Received(1).LoadAsync("shop", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalidate_ForcesReload()
    {
        var inner = Substitute.For<IRelationalSchemaReader>();
        inner.LoadAsync("shop", Arg.Any<CancellationToken>()).Returns(_ => Schema("shop"));
        var reader = new CachingSchemaReader(inner);

        await reader.LoadAsync("shop", CancellationToken.None);
        reader.Invalidate("shop");
        await reader.LoadAsync("shop", CancellationToken.None);

        await inner.Received(2).LoadAsync("shop", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAll_ClearsEveryNamespace()
    {
        var inner = Substitute.For<IRelationalSchemaReader>();
        inner.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Schema(ci.Arg<string>()));
        var reader = new CachingSchemaReader(inner);

        await reader.LoadAsync("a", CancellationToken.None);
        await reader.LoadAsync("b", CancellationToken.None);
        reader.InvalidateAll();
        await reader.LoadAsync("a", CancellationToken.None);
        await reader.LoadAsync("b", CancellationToken.None);

        await inner.Received(2).LoadAsync("a", Arg.Any<CancellationToken>());
        await inner.Received(2).LoadAsync("b", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailedLoad_IsNotCached()
    {
        var inner = Substitute.For<IRelationalSchemaReader>();
        inner.LoadAsync("shop", Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new InvalidOperationException("boom"),
                _ => Schema("shop"));
        var reader = new CachingSchemaReader(inner);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => reader.LoadAsync("shop", CancellationToken.None));
        var recovered = await reader.LoadAsync("shop", CancellationToken.None);

        Assert.Equal("shop", recovered.Name);
        await inner.Received(2).LoadAsync("shop", Arg.Any<CancellationToken>());
    }
}
