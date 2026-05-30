using Lexql.App.Services;
using Lexql.Core.Abstractions;
using Lexql.Core.Connections;
using Lexql.Core.History;
using Lexql.Providers.MySql;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Lexql.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
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
		builder.Services.AddSingleton<IQueryHistoryStore>(
			_ => new SqliteQueryHistoryStore(Path.Combine(FileSystem.AppDataDirectory, "history.db")));

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
