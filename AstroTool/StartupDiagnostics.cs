namespace AstroTool;

internal static class StartupDiagnostics
{
	private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "AstroTool-startup.log");

	public static void Write(string message)
	{
		try
		{
			File.AppendAllText(LogPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
		}
		catch
		{
			// Ignore logging failures.
		}
	}
}