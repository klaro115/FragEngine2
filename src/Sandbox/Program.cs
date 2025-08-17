using FragEngine.EngineCore;
using FragEngine.Helpers;
using FragEngine.Logging;
using Sandbox.Application;

Console.WriteLine("\n### BEGIN ###\n");

Engine? engine = null;
ConsoleLogger? logger = null;
try
{
	logger = new()
	{
		MaxErrorSeverityForExit = LogEntrySeverity.Fatal,
	};
	logger.LogMessage("Creating engine...");

	TestAppLogic appLogic = new();

	if (EngineStartupHelper.CreateDefaultEngine(appLogic, logger, out engine))
	{
		logger.FatalErrorOccurred += (_) => engine?.RequestExit();
		logger.LogMessage("Starting engine...");

		engine!.Run();

		logger.LogMessage("Engine has exited.");
	}
}
catch (Exception ex)
{
	if (logger is not null)
	{
		logger.LogException("FATAL: Unhandled exception caused engine crash!", ex, LogEntrySeverity.Fatal);
	}
	else
	{
		Console.WriteLine($"FATAL: Unhandled exception caused engine crash!\nException: {ex}");
	}
}
finally
{
	engine?.Dispose();
	logger?.Dispose();
}

Console.WriteLine("\n#### END ####");
