namespace Lexql.App.Services;

public sealed record FilePick(string Path, string Content);

public interface IFileDialog
{
    bool Supported { get; }

    Task<FilePick?> OpenAsync();

    Task<string?> SaveAsync(string suggestedName, string content);
}

public sealed class NoopFileDialog : IFileDialog
{
    public bool Supported => false;

    public Task<FilePick?> OpenAsync() => Task.FromResult<FilePick?>(null);

    public Task<string?> SaveAsync(string suggestedName, string content) => Task.FromResult<string?>(null);
}
