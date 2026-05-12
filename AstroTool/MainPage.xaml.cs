namespace AstroTool;

using Microsoft.AspNetCore.Components.WebView.Maui;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		StartupDiagnostics.Write("MainPage: ctor start");
		InitializeComponent();

		try
		{
			StartupDiagnostics.Write("MainPage: creating BlazorWebView");
			var blazorWebView = new BlazorWebView
			{
				HostPage = "wwwroot/index.html"
			};

			blazorWebView.RootComponents.Add(new RootComponent
			{
				Selector = "#app",
				ComponentType = typeof(Components.Routes)
			});

			Content = blazorWebView;
			StartupDiagnostics.Write("MainPage: content assigned");
		}
		catch (Exception ex)
		{
			StartupDiagnostics.Write($"MainPage: failed {ex.GetType().Name} {ex.Message}");
			var hresult = $"0x{ex.HResult:X8}";
			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Padding = new Thickness(24),
					Spacing = 12,
					Children =
					{
						new Label
						{
							Text = "Unable to initialize embedded web view",
							FontSize = 22,
							FontAttributes = FontAttributes.Bold
						},
						new Label
						{
							Text =
								"Install or repair Microsoft Edge WebView2 Runtime and relaunch AstroTool.\n" +
								"Download: https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" +
								$"Error: {ex.GetType().Name} ({hresult})\n{ex.Message}",
							LineBreakMode = LineBreakMode.WordWrap
						}
					}
				}
			};
		}
	}
}
