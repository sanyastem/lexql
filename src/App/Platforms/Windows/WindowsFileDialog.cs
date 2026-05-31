using Windows.Storage;
using Windows.Storage.Pickers;
using MauiApplication = Microsoft.Maui.Controls.Application;
using NativeWindow = Microsoft.UI.Xaml.Window;

namespace Lexql.App.Services;

public sealed class WindowsFileDialog : IFileDialog
{
    public bool Supported => true;

    public async Task<FilePick?> OpenAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".sql");
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add("*");
        Bind(picker);

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return null;
        }

        var text = await FileIO.ReadTextAsync(file);
        return new FilePick(file.Path, text);
    }

    public async Task<string?> SaveAsync(string suggestedName, string content)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = suggestedName,
        };
        picker.FileTypeChoices.Add("SQL", [".sql"]);
        Bind(picker);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return null;
        }

        await FileIO.WriteTextAsync(file, content);
        return file.Path;
    }

    private static void Bind(object picker)
    {
        if (Native() is { } window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
    }

    private static NativeWindow? Native()
    {
        var windows = MauiApplication.Current?.Windows;
        if (windows is null || windows.Count == 0)
        {
            return null;
        }

        return windows[0].Handler?.PlatformView as NativeWindow;
    }
}
