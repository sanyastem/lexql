namespace Lexql.App.Services;

public interface IWindowControls
{
    bool Supported { get; }

    void Minimize();

    void ToggleMaximize();

    void Close();

    int[] GetPosition();

    void MoveTo(int x, int y);
}

public sealed class NoopWindowControls : IWindowControls
{
    public bool Supported => false;

    public void Minimize()
    {
    }

    public void ToggleMaximize()
    {
    }

    public void Close()
    {
    }

    public int[] GetPosition() => [0, 0];

    public void MoveTo(int x, int y)
    {
    }
}
