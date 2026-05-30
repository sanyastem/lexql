namespace Lexql.Core.Connections;

public interface ILiveConnection : IAsyncDisposable
{
    string ProviderId { get; }
    ServerVersionInfo Server { get; }
}
