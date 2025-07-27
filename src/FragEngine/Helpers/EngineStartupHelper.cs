using FragEngine.EngineCore;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.Helpers;

/// <summary>
/// Helper class for getting the engine started.
/// </summary>
public static class EngineStartupHelper
{
	#region Constants

	private const int minServiceCount = 4;	// update value as needed.

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

		if (!CreateDefaultServiceProvider(services, out IServiceProvider? serviceProvider))
		{
			Console.WriteLine("Failed to create service provider during default engine creation!");
			_outEngine = null;
			return false;
		}

		try
		{
			_outEngine = new(serviceProvider);
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

		//TODO: Load engine config file from file.

		try
		{
			PlatformService platformService = new(_loggerInstance);

			_outServices = new ServiceCollection()
				.AddSingleton(_loggerInstance)
				.AddSingleton(platformService)
				.AddSingleton<WindowService>()
				.AddSingleton<GraphicsService>();
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

	#endregion
}
