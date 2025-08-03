using FragEngine.EngineCore.Input;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.EngineCore;

/// <summary>
/// Extension method for <see cref="IServiceCollection"/>, that add engine services to the DI.
/// </summary>
public static class EngineServiceCollectionExt
{
	#region Constants

	public const int defaultServiceCount = 6;

	#endregion
	#region Methods

	public static IServiceCollection UseEngine(this IServiceCollection _serviceCollection)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);

		ConsoleLogger loggerInstance = new();
		return UseEngine(_serviceCollection, loggerInstance);
	}

	public static IServiceCollection UseEngine(this IServiceCollection _serviceCollection, ILogger _loggerInstance)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);
		ArgumentNullException.ThrowIfNull(_loggerInstance);

		PlatformService platformService = new(_loggerInstance);

		_serviceCollection
			.AddSingleton(_loggerInstance)
			.AddSingleton(platformService)
			.AddSingleton<TimeService>()
			.AddSingleton<InputService>()
			.AddSingleton<WindowService>();

		return _serviceCollection;
	}

	public static IServiceCollection UseEngine(this IServiceCollection _serviceCollection, ILogger _loggerInstance, EngineConfig _config)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);
		ArgumentNullException.ThrowIfNull(_loggerInstance);
		ArgumentNullException.ThrowIfNull(_config);

		_serviceCollection
			.UseEngine(_loggerInstance)
			.AddSingleton(_config);

		return _serviceCollection;
	}

	#endregion
}
