namespace Lexql.Core.Connections;

public sealed class UnknownProviderException : Exception
{
    public UnknownProviderException(string providerId)
        : base($"No connection opener is registered for provider '{providerId}'.")
    {
        ProviderId = providerId;
    }

    public string ProviderId { get; }
}
