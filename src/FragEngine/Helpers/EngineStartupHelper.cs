using FragEngine.EngineCore;
using FragEngine.Graphics;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace FragEngine.Helpers;

/// <summary>
/// Helper class for getting the engine started.
/// </summary>
public static class EngineStartupHelper
{
	#region Constants

	private const int minServiceCount =
		EngineServiceCollectionExt.defaultServiceCount +
		GraphicsServiceCollectionExt.defaultServiceCount;

	#endregion
	#region Methods

	/// <summary>
	/// Creates a new engine instance using default services and settings.
	/// </summary>
	/// <param name="_outEngine">Outputs a new engine instance, or null on failure.</param>
	/// <returns>True if the engine was created successfully, false otherwise.</returns>
	public static bool CreateDefaultEngine(out Engine? _outEngine)
	{
		if (!CreateDefaultServiceCollection(out IServiceCollection? services))
		{
			Console.WriteLine("Failed to create service collection during default engine creation!");
			_outEngine = null;
			return false;
		}

		try
		{
			_outEngine = new(services);
			return !_outEngine.IsDisposed;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to complete default engine creation!\nException: {ex}'");
			_outEngine = null;
			return false;
		}
	}

	internal static bool CreateDefaultServiceProvider(IServiceCollection? _services, out IServiceProvider? _outServiceProvider)
	{
		if (_services is null || _services.Count < minServiceCount)
		{
			Console.WriteLine("Cannot create default service provider using null or incomplete service collection!");
			_outServiceProvider = null;
			return false;
		}

		try
		{
			_outServiceProvider = _services!.BuildServiceProvider();
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to create default service provider!\nException: {ex}'");
			_outServiceProvider = null;
			return false;
		}

	}

	/// <summary>
	/// Create a default service collection for engine creation, including only the core services.
	/// </summary>
	/// <param name="_outServices">Outputs the service collection, or null on error.</param>
	/// <returns>True if the service collection was created successfully, false otherwise.</returns>
	public static bool CreateDefaultServiceCollection(out IServiceCollection? _outServices)
	{
		ConsoleLogger logger = new();
		return CreateDefaultServiceCollection(logger, out _outServices);
	}

	/// <summary>
	/// Create a default service collection for engine creation, including only the core services, but using a custom logger.
	/// </summary>
	/// <param name="_loggerInstance">An instance of the logging service implementation.</param>
	/// <param name="_outServices">Outputs the service collection, or null on error.</param>
	/// <returns>True if the service collection was created successfully, false otherwise.</returns>
	public static bool CreateDefaultServiceCollection(ILogger _loggerInstance, out IServiceCollection? _outServices)
	{
		if (_loggerInstance is null || _loggerInstance.IsDisposed)
		{
			Console.WriteLine("Cannot create default service collection using null or disposed logging service!");
			_outServices = null;
			return false;
		}

		// Load engine config file from file:
		if (!LoadEngineConfig(out EngineConfig config))
		{
			_loggerInstance.LogWarning("Failed to load engine config from file, using default config instead.");
		}

		try
		{
			_outServices = new ServiceCollection()
				.UseEngine(_loggerInstance, config)
				.UseGraphics();
				//...

			return true;
		}
		catch (Exception ex)
		{
			_loggerInstance.LogException("Failed to create default engine service collection!", ex, LogEntrySeverity.Critical);
			_outServices = null;
			return false;
		}
	}

	/// <summary>
	/// Try to load and parse the engine's config from a JSON file.
	/// </summary>
	/// <param name="_outConfig">Outputs the loaded config. If not found or on error, the default config is returned instead.</param>
	/// <returns>True if the config was loaded from file, false otherwise.</returns>
	public static bool LoadEngineConfig(out EngineConfig _outConfig)
	{
		string rootDirPath = PlatformService.GetRootDirectoryPath();
		string configFilePath = Path.Combine(rootDirPath, "engineconfig.json");

		if (!File.Exists(configFilePath))
		{
			Console.WriteLine($"Engine config JSON file could not be found. (File path: '{configFilePath}')");
			_outConfig = EngineConfig.CreateDefault();
			return false;
		}

		EngineConfig? config;
		try
		{
			string configJson = File.ReadAllText(configFilePath);
			config = JsonSerializer.Deserialize<EngineConfig>(configJson);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to load engine config JSON from file!\nFile path: '{configFilePath}'\nException: {ex}'");
			_outConfig = EngineConfig.CreateDefault();
			return false;
		}

		_outConfig = config ?? EngineConfig.CreateDefault();
		return true;

	}

	#endregion
}
