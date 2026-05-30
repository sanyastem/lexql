namespace Lexql.Core.Connections;

public sealed class InvalidConnectionProfileException : Exception
{
    public InvalidConnectionProfileException(string profileName, IReadOnlyList<string> errors)
        : base($"Connection profile '{profileName}' is invalid: {string.Join(" ", errors)}")
    {
        ProfileName = profileName;
        Errors = errors;
    }

    public string ProfileName { get; }

    public IReadOnlyList<string> Errors { get; }
}
