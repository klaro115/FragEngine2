using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.Helpers;
using FragEngine.Logging;
using Sandbox.Application;

Console.WriteLine("\n### BEGIN ###\n");

EngineConfig engineConfig = new()
{
	Startup = new()
	{
		CreateMainWindowImmediately = true,
	},
	Graphics = new()
	{
		PreferNativeGraphicsAPI = true, // On Windows: false=Vulkan, true=Dx11
	},
	Optimizations = new(),
};

Engine? engine = null;
ConsoleLogger? logger = null;
try
{
	logger = new()
	{
		MaxErrorSeverityForExit = LogEntrySeverity.Fatal,
	};
	logger.LogMessage("Creating engine...");

	using TestAppLogic appLogic = new();

	if (EngineStartupHelper.CreateDefaultEngine(appLogic, logger, engineConfig, out engine))
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
