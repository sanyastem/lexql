using Lexql.App.Services;
using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using Lexql.Core.History;
using Lexql.Providers.MySql;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
#if WINDOWS
using Microsoft.Maui.Platform;
#endif

namespace Lexql.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		InstallCrashHandlers();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		builder.Services.AddSingleton<IDatabaseProvider, MySqlDatabaseProvider>();
		builder.Services.AddSingleton<ISecretProtector>(_ => new AesSecretProtector(KeyStore.GetOrCreateKey()));
		builder.Services.AddSingleton<IConnectionProfileStore>(sp =>
		{
			var provider = sp.GetRequiredService<IDatabaseProvider>();
			var secretKeys = provider.DescribeConnectionFields()
				.Where(f => f.Kind == ConnectionFieldKind.Password)
				.Select(f => f.Key)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
			var path = Path.Combine(FileSystem.AppDataDirectory, "connections.json");
			return new JsonConnectionProfileStore(path, secretKeys, sp.GetRequiredService<ISecretProtector>());
		});
		builder.Services.AddSingleton<WorkspaceState>();
		builder.Services.AddSingleton<LocalizationService>();
		builder.Services.AddSingleton<ThemeState>();
		builder.Services.AddSingleton<IQueryHistoryStore>(
			_ => new SqliteQueryHistoryStore(Path.Combine(FileSystem.AppDataDirectory, "history.db")));

#if WINDOWS
		builder.Services.AddSingleton<IWindowControls, WindowsWindowControls>();
		builder.Services.AddSingleton<IFileDialog, WindowsFileDialog>();
		ConfigureFramelessWindow();
#else
		builder.Services.AddSingleton<IWindowControls, NoopWindowControls>();
		builder.Services.AddSingleton<IFileDialog, NoopFileDialog>();
#endif

		builder.Logging.AddProvider(new FileLoggerProvider());
		builder.Logging.SetMinimumLevel(LogLevel.Warning);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void InstallCrashHandlers()
	{
		CrashLog.Append($"=== Lexql start === log: {CrashLog.Path}");

		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			CrashLog.Append("[AppDomain.UnhandledException] " + e.ExceptionObject);

		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			CrashLog.Append("[UnobservedTaskException] " + e.Exception);
			e.SetObserved();
		};

#if WINDOWS
		if (Microsoft.UI.Xaml.Application.Current is { } app)
		{
			app.UnhandledException += (_, e) =>
				CrashLog.Append($"[XAML.UnhandledException] {e.Message}{Environment.NewLine}{e.Exception}");
		}
#endif
	}

#if WINDOWS
	private static void ConfigureFramelessWindow()
	{
		Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("LexqlFrameless", (handler, view) =>
		{
			var native = handler.PlatformView;
			ApplyFrameless(native, "mapper");
			var hooked = false;
			native.Activated += (_, _) =>
			{
				ApplyFrameless(native, "activated");
				FixChrome(native);
				if (!hooked && native.Content is Microsoft.UI.Xaml.FrameworkElement content)
				{
					hooked = true;
					content.LayoutUpdated += (_, _) => FixChrome(native);
				}
			};
		});
	}

	private static Microsoft.UI.Xaml.FrameworkElement? _contentGrid;
	private static Microsoft.UI.Xaml.FrameworkElement? _appTitleBar;

	private static void FixChrome(Microsoft.UI.Xaml.Window native)
	{
		try
		{
			if ((_contentGrid is null || _appTitleBar is null) &&
				native.Content is Microsoft.UI.Xaml.DependencyObject root)
			{
				FindChrome(root);
			}

			if (_appTitleBar is { Visibility: not Microsoft.UI.Xaml.Visibility.Collapsed })
			{
				_appTitleBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
			}

			if (_contentGrid is not null && _contentGrid.Margin.Top != 0)
			{
				_contentGrid.Margin = new Microsoft.UI.Xaml.Thickness(0);
			}
		}
		catch
		{
		}
	}

	private static void FindChrome(Microsoft.UI.Xaml.DependencyObject node)
	{
		var children = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node);
		for (var i = 0; i < children; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node, i);
			if (child is Microsoft.UI.Xaml.FrameworkElement { Name: "ContentGrid" } contentGrid)
			{
				_contentGrid = contentGrid;
			}
			else if (child is Microsoft.UI.Xaml.FrameworkElement { Name: "AppTitleBarContainer" } titleBar)
			{
				_appTitleBar = titleBar;
			}

			FindChrome(child);
		}
	}

	private static void ApplyFrameless(Microsoft.UI.Xaml.Window native, string stage)
	{
		try
		{
			var appWindow = native.GetAppWindow();
			if (appWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
			{
				presenter.SetBorderAndTitleBar(true, false);
			}
		}
		catch (Exception ex)
		{
			CrashLog.Append($"Frameless({stage}) failed: " + ex);
		}
	}
#endif
}
