using Microsoft.UI.Windowing;
using MauiApplication = Microsoft.Maui.Controls.Application;
using NativeWindow = Microsoft.UI.Xaml.Window;

namespace Lexql.App.Services;

public sealed class WindowsWindowControls : IWindowControls
{
    public bool Supported => true;

    public void Minimize() => Presenter()?.Minimize();

    public void ToggleMaximize()
    {
        if (Presenter() is not { } presenter)
        {
            return;
        }

        if (presenter.State == OverlappedPresenterState.Maximized)
        {
            presenter.Restore();
        }
        else
        {
            presenter.Maximize();
        }
    }

    public void Close() => Native()?.Close();

    public int[] GetPosition()
    {
        var position = Native()?.AppWindow.Position;
        return position is { } p ? [p.X, p.Y] : [0, 0];
    }

    public void MoveTo(int x, int y) => Native()?.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));

    private static NativeWindow? Native()
    {
        var windows = MauiApplication.Current?.Windows;
        if (windows is null || windows.Count == 0)
        {
            return null;
        }

        return windows[0].Handler?.PlatformView as NativeWindow;
    }

    private static OverlappedPresenter? Presenter() => Native()?.AppWindow.Presenter as OverlappedPresenter;
}
