using FragEngine.Application;
using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.Graphics;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources;
using FragEngine.Resources.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace FragEngine.Helpers;

/// <summary>
/// Helper class for getting the engine started.
/// </summary>
public static class EngineStartupHelper
{
	#region Constants

	private const int minServiceCount =
		EngineServiceCollectionExt.defaultServiceCount +
		GraphicsServiceCollectionExt.defaultServiceCount +
		ResourcesServiceCollectionExt.defaultServiceCount;

	#endregion
	#region Methods

	/// <summary>
	/// Creates a new engine instance using default services and settings.
	/// </summary>
	/// <param name="_appLogic">An application logic instance that will control the engine.</param>
	/// <param name="_outEngine">Outputs a new engine instance, or null on failure.</param>
	/// <returns>True if the engine was created successfully, false otherwise.</returns>
	public static bool CreateDefaultEngine(IAppLogic _appLogic, [NotNullWhen(true)] out Engine? _outEngine)
	{
		ConsoleLogger logger = new();
		return CreateDefaultEngine(_appLogic, logger, out _outEngine);
	}

	/// <summary>
	/// Creates a new engine instance using default services and settings.
	/// </summary>
	/// <param name="_appLogic">An application logic instance that will control the engine.</param>
	/// <param name="_loggerInstance">A logger that shall be added to the engine as its primary logging service singleton.</param>
	/// <param name="_outEngine">Outputs a new engine instance, or null on failure.</param>
	/// <returns>True if the engine was created successfully, false otherwise.</returns>
	public static bool CreateDefaultEngine(IAppLogic _appLogic, ILogger _loggerInstance, [NotNullWhen(true)] out Engine? _outEngine)
	{
		if (_loggerInstance is null || _loggerInstance.IsDisposed)
		{
			Console.WriteLine("Cannot create default engine using null or disposed logging service!");
			_outEngine = null;
			return false;
		}

		if (_appLogic is null || (_appLogic is IExtendedDisposable disposable && disposable.IsDisposed))
		{
			_loggerInstance.LogError("Cannot create default engine using null or disposed app logic!");
			_outEngine = null;
			return false;
		}

		if (!CreateDefaultServiceCollection(_loggerInstance, out IServiceCollection? services))
		{
			_loggerInstance.LogError("Failed to create service collection during default engine creation!", LogEntrySeverity.Critical);
			_outEngine = null;
			return false;
		}

		try
		{
			_outEngine = new(_appLogic, services);
			return !_outEngine.IsDisposed;
		}
		catch (Exception ex)
		{
			_loggerInstance.LogException($"Failed to complete default engine creation!", ex, LogEntrySeverity.Fatal);
			_outEngine = null;
			return false;
		}
	}

	/// <summary>
	/// Creates a new engine instance using default services and settings.
	/// </summary>
	/// <param name="_appLogic">An application logic instance that will control the engine.</param>
	/// <param name="_loggerInstance">A logger that shall be added to the engine as its primary logging service singleton.</param>
	/// <param name="_engineConfig">A custom engine configuration.</param>
	/// <param name="_outEngine">Outputs a new engine instance, or null on failure.</param>
	/// <returns>True if the engine was created successfully, false otherwise.</returns>
	public static bool CreateDefaultEngine(IAppLogic _appLogic, ILogger _loggerInstance, EngineConfig _engineConfig, [NotNullWhen(true)] out Engine? _outEngine)
	{
		if (_loggerInstance is null || _loggerInstance.IsDisposed)
		{
			Console.WriteLine("Cannot create default engine using null or disposed logging service!");
			_outEngine = null;
			return false;
		}

		if (_appLogic is null || (_appLogic is IExtendedDisposable disposable && disposable.IsDisposed))
		{
			_loggerInstance.LogError("Cannot create default engine using null or disposed app logic!");
			_outEngine = null;
			return false;
		}

		if (!CreateDefaultServiceCollection(_loggerInstance, _engineConfig, out IServiceCollection? services))
		{
			_loggerInstance.LogError("Failed to create service collection during default engine creation!", LogEntrySeverity.Critical);
			_outEngine = null;
			return false;
		}

		try
		{
			_outEngine = new(_appLogic, services);
			return !_outEngine.IsDisposed;
		}
		catch (Exception ex)
		{
			_loggerInstance.LogException($"Failed to complete default engine creation!", ex, LogEntrySeverity.Fatal);
			_outEngine = null;
			return false;
		}
	}

	internal static bool CreateDefaultServiceProvider(IServiceCollection? _services, [NotNullWhen(true)] out IServiceProvider? _outServiceProvider)
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
	public static bool CreateDefaultServiceCollection(ILogger _loggerInstance, [NotNullWhen(true)] out IServiceCollection? _outServices)
	{
		if (_loggerInstance is null || _loggerInstance.IsDisposed)
		{
			Console.WriteLine("Cannot create default service collection using null or disposed logging service!");
			_outServices = null;
			return false;
		}

		// Load engine config file from file:
		if (!LoadEngineConfig(_loggerInstance, out EngineConfig config))
		{
			_loggerInstance.LogWarning("Failed to load engine config from file, using default config instead.");
		}

		return CreateDefaultServiceCollection(_loggerInstance, config, out _outServices);
	}

	/// <summary>
	/// Create a default service collection for engine creation, including only the core services, but using a custom logger and config.
	/// </summary>
	/// <param name="_loggerInstance">An instance of the logging service implementation.</param>
	/// <param name="_engineConfig">A custom engine configuration.</param>
	/// <param name="_outServices">Outputs the service collection, or null on error.</param>
	/// <returns>True if the service collection was created successfully, false otherwise.</returns>
	public static bool CreateDefaultServiceCollection(ILogger _loggerInstance, EngineConfig _engineConfig, [NotNullWhen(true)] out IServiceCollection? _outServices)
	{
		if (_loggerInstance is null || _loggerInstance.IsDisposed)
		{
			Console.WriteLine("Cannot create default service collection using null or disposed logging service!");
			_outServices = null;
			return false;
		}

		if (_engineConfig is null || !_engineConfig.IsValid())
		{
			Console.WriteLine("Cannot create default service collection using null or invalid engine config!");
			_outServices = null;
			return false;
		}

		try
		{
			_outServices = new ServiceCollection()
				.UseEngine(_loggerInstance, _engineConfig!)
				.UseGraphics()
				.UseResources();
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
	/// <param name="_loggerInstance">An instance of the logging service implementation. If null, a default logger is used instead.</param>
	/// <param name="_outConfig">Outputs the loaded config. If not found or on error, the default config is returned instead.</param>
	/// <returns>True if the config was loaded from file, false otherwise.</returns>
	public static bool LoadEngineConfig(ILogger? _loggerInstance, [NotNull] out EngineConfig _outConfig)
	{
		_loggerInstance ??= new ConsoleLogger();

		string rootDirPath = PlatformService.GetRootDirectoryPath();
		string configFilePath = Path.Combine(rootDirPath, "engineconfig.json");

		if (!File.Exists(configFilePath))
		{
			_loggerInstance.LogError($"Engine config JSON file could not be found. (File path: '{configFilePath}')");
			_outConfig = EngineConfig.CreateDefault();
			return false;
		}

		SerializerService serializer = new(_loggerInstance, new());

		EngineConfig? config;
		FileStream? fileStream = null;
		try
		{
			JsonTypeInfo<EngineConfig> typeInfo = EngineCoreJsonContext.Default.EngineConfig;

			fileStream = new(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

			if (!serializer.DeserializeFromJson(fileStream, typeInfo, out config))
			{
				_outConfig = EngineConfig.CreateDefault();
				return false;
			}
		}
		catch (Exception ex)
		{
			_loggerInstance.LogException($"Failed to load engine config JSON from file! (File path: '{configFilePath}')", ex, LogEntrySeverity.Normal);
			_outConfig = EngineConfig.CreateDefault();
			return false;
		}
		finally
		{
			fileStream?.Close();
		}

		if (config is null)
		{
			_loggerInstance.LogWarning($"Engine config JSON was empty, using default values.");
			config ??= EngineConfig.CreateDefault();
		}

		_outConfig = config;
		return true;
	}

	#endregion
}
