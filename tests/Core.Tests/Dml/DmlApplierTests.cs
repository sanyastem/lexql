using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Dml;
using NSubstitute;

namespace Lexql.Core.Tests.Dml;

public class DmlApplierTests
{
    private static QueryExecution Empty() => new() { ResultSets = [] };

    private static IQueryExecutor Executor()
    {
        var executor = Substitute.For<IQueryExecutor>();
        executor.ExecuteAsync(Arg.Any<QueryRequest>(), Arg.Any<CancellationToken>()).Returns(Empty());
        return executor;
    }

    private static DmlCommand Cmd(string sql) => new(sql, []);

    [Fact]
    public async Task Apply_WrapsCommandsInTransactionAndCommits()
    {
        var executor = Executor();
        var commands = new[] { Cmd("UPDATE a"), Cmd("DELETE b") };

        await DmlApplier.ApplyAsync(executor, commands, CancellationToken.None);

        Received.InOrder(() =>
        {
            executor.ExecuteAsync(Arg.Is<QueryRequest>(r => r.Text == "START TRANSACTION"), Arg.Any<CancellationToken>());
            executor.ExecuteAsync(Arg.Is<QueryRequest>(r => r.Text == "UPDATE a"), Arg.Any<CancellationToken>());
            executor.ExecuteAsync(Arg.Is<QueryRequest>(r => r.Text == "DELETE b"), Arg.Any<CancellationToken>());
            executor.ExecuteAsync(Arg.Is<QueryRequest>(r => r.Text == "COMMIT"), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Apply_RollsBackOnError()
    {
        var executor = Executor();
        executor.ExecuteAsync(Arg.Is<QueryRequest>(r => r.Text == "BAD"), Arg.Any<CancellationToken>())
            .Returns<QueryExecution>(_ => throw new QueryExecutionException("boom", 1, null, null));

        await Assert.ThrowsAsync<QueryExecutionException>(
            () => DmlApplier.ApplyAsync(executor, [Cmd("BAD")], CancellationToken.None));

        await executor.Received().ExecuteAsync(
            Arg.Is<QueryRequest>(r => r.Text == "ROLLBACK"), Arg.Any<CancellationToken>());
        await executor.DidNotReceive().ExecuteAsync(
            Arg.Is<QueryRequest>(r => r.Text == "COMMIT"), Arg.Any<CancellationToken>());
    }
}
