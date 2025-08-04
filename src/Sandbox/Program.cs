using FragEngine.EngineCore;
using FragEngine.Helpers;
using Sandbox.Application;

Console.WriteLine("\n### BEGIN ###\n");

Engine? engine = null;
try
{
	Console.WriteLine("Creating engine...");

	TestAppLogic appLogic = new();

	if (EngineStartupHelper.CreateDefaultEngine(appLogic, out engine))
	{
		Console.WriteLine("Starting engine...");

		engine!.Run();

		Console.WriteLine("Engine has exited.");
	}
}
catch (Exception ex)
{
	Console.WriteLine($"FATAL: Unhandled exception caused engine crash!\nException: {ex}");
}
finally
{
	engine?.Dispose();
}

Console.WriteLine("\n#### END ####");
