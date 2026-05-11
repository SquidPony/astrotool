using AstroTool.Core.Services;
using AstroTool.Services;
using Microsoft.Extensions.Logging;

namespace AstroTool;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		StartupDiagnostics.Write("MauiProgram.CreateMauiApp: start");
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		StartupDiagnostics.Write("MauiProgram.CreateMauiApp: adding blazor webview");
		builder.Services.AddMauiBlazorWebView();

		// Register astronomy services
		builder.Services.AddSingleton<AstronomyService>();
		builder.Services.AddSingleton<LocationService>();
		builder.Services.AddSingleton<SensorService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		StartupDiagnostics.Write("MauiProgram.CreateMauiApp: built");
		return app;
	}
}
