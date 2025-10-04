using FragEngine.Application;
using FragEngine.EngineCore.Config;
using FragEngine.Extensions;
using FragEngine.Helpers;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.EngineCore.Internal;

/// <summary>
/// Helper class for setting up the engine's DI system.
/// </summary>
internal static class EngineServiceSetupHelper
{
	#region Methods

	/// <summary>
	/// Initialize the engine's dependency injection, and create the final service provider.
	/// </summary>
	/// <param name="_engine">The engine for which we're creating DI services.</param>
	/// <param name="_appLogic">The engine's application logic singleton.</param>
	/// <param name="_serviceCollection">Optional. A customized collection of services. If null, the necessary minimum
	/// services are used. If any key services are missing from a custom collection, they are substituted by standard
	/// implementations.</param>
	/// <param name="_outServiceProvider">Outputs the engine's final servive provider, or null, if initialization fails.</param>
	/// <returns>True if the DI system was initialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Engine and app logic may not be null.</exception>
	public static bool InitializeDependencyInjection(
		Engine _engine,
		IAppLogic _appLogic,
		IServiceCollection? _serviceCollection,
		[NotNullWhen(true)] out IServiceProvider? _outServiceProvider)
	{
		ArgumentNullException.ThrowIfNull(_engine);
		ArgumentNullException.ThrowIfNull(_appLogic);

		// If no customized service collection is provided, use a basic default setup:
		if (_serviceCollection is null && !EngineStartupHelper.CreateDefaultServiceCollection(out _serviceCollection!))
		{
			_outServiceProvider = null;
			return false;
		}

		// Ensure that a logging service is registered; if an implementation instance exists, use that for error logging:
		ILogger? diLogger;
		if (!_serviceCollection.HasService<ILogger>())
		{
			diLogger = new ConsoleLogger();
			_serviceCollection.AddSingleton(diLogger);
		}
		else if ((diLogger = _serviceCollection.GetImplementationInstance<ILogger>()) is null)
		{
			diLogger = new ConsoleLogger();
		}

		// Load or create the main engine config, and register it as a singleton:
		if (!_serviceCollection.HasService<EngineConfig>() && EngineStartupHelper.LoadEngineConfig(diLogger, out EngineConfig config))
		{
			_serviceCollection.AddSingleton(config);
		}
		else
		{
			config = _serviceCollection.GetImplementationInstance<EngineConfig>()!;
		}
		if (!config.IsValid())
		{
			diLogger.LogError("Cannot initialize dependency injection using invalid engine config!", LogEntrySeverity.Critical);
			_outServiceProvider = null;
			return false;
		}

		// If requested, add application logic as a service; remove it otherwise:
		if (config.Startup.AddAppLogicToServiceProvider && !_serviceCollection.HasService<IAppLogic>())
		{
			_serviceCollection.AddSingleton(_appLogic);
		}
		else if (_serviceCollection.HasService<IAppLogic>())
		{
			_serviceCollection.RemoveAll<EngineConfig>();
		}

		// Add the engine itself as a singleton:
		_serviceCollection.AddSingleton(_engine);

		// Create the final service provider for the engine-wide DI:
		if (!EngineStartupHelper.CreateDefaultServiceProvider(_serviceCollection, out _outServiceProvider))
		{
			return false;
		}

		return true;
	}

	#endregion
}
