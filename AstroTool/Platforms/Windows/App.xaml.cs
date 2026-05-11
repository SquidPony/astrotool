using Microsoft.UI.Xaml;

namespace AstroTool.WinUI;

public partial class App : MauiWinUIApplication
{
    static App()
    {
        StartupDiagnostics.Write("WinUI App: static ctor");
    }

    public App()
    {
        StartupDiagnostics.Write("WinUI App: ctor start");
        this.InitializeComponent();
        StartupDiagnostics.Write("WinUI App: ctor end");
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
