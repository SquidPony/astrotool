namespace AstroTool.WinUI;

using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinRT;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		WinRT.ComWrappersSupport.InitializeComWrappers();

		Bootstrap.Initialize(0x00010007);

		Application.Start(_ =>
		{
			var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
				Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
			System.Threading.SynchronizationContext.SetSynchronizationContext(context);
			new App();
		});

		Bootstrap.Shutdown();
	}
}