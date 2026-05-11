namespace AstroTool;

public partial class App : Application
{
	public App()
	{
		StartupDiagnostics.Write("AstroTool.App: ctor");
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		StartupDiagnostics.Write("AstroTool.App: CreateWindow start");
		try
		{
			var window = new Window(new MainPage()) { Title = "AstroTool" };
			StartupDiagnostics.Write("AstroTool.App: CreateWindow success");
			return window;
		}
		catch (Exception ex)
		{
			StartupDiagnostics.Write($"AstroTool.App: CreateWindow failed: {ex.GetType().Name} {ex.Message}");
			return new Window(CreateStartupErrorPage(ex)) { Title = "AstroTool" };
		}
	}

	private static Page CreateStartupErrorPage(Exception ex)
	{
		var hresult = $"0x{ex.HResult:X8}";
		var details =
			"AstroTool could not initialize the embedded web view.\n\n" +
			"This commonly happens when required WebView runtime components are missing on the machine.\n\n" +
			"Install or repair Microsoft Edge WebView2 Runtime, then relaunch the app.\n" +
			"Download: https://go.microsoft.com/fwlink/p/?LinkId=2124703\n\n" +
			$"Error: {ex.GetType().Name} ({hresult})\n" +
			ex.Message;

		return new ContentPage
		{
			Padding = new Thickness(24),
			Content = new ScrollView
			{
				Content = new VerticalStackLayout
				{
					Spacing = 12,
					Children =
					{
						new Label
						{
							Text = "Startup Issue",
							FontSize = 24,
							FontAttributes = FontAttributes.Bold
						},
						new Label
						{
							Text = details,
							LineBreakMode = LineBreakMode.WordWrap
						}
					}
				}
			}
		};
	}
}
