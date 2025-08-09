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

	/// <summary>
	/// Adds services for basic engine operation.
	/// </summary>
	/// <remarks>
	/// This will add a generic logging service of type '<see cref="ConsoleLogger"/>'.
	/// </remarks>
	/// <param name="_serviceCollection">This service collection.</param>
	/// <param name="_config">The engine configuration.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection UseEngine(this IServiceCollection _serviceCollection, EngineConfig _config)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);

		ConsoleLogger loggerInstance = new();
		return UseEngine(_serviceCollection, loggerInstance, _config);
	}

	/// <summary>
	/// Adds services for basic engine operation.
	/// </summary>
	/// <param name="_serviceCollection">This service collection.</param>
	/// <param name="_loggerInstance">A custom logging service instance.</param>
	/// <param name="_config">The engine configuration.</param>
	/// <returns>The updated service collection.</returns>
	public static IServiceCollection UseEngine(this IServiceCollection _serviceCollection, ILogger _loggerInstance, EngineConfig _config)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);
		ArgumentNullException.ThrowIfNull(_loggerInstance);
		ArgumentNullException.ThrowIfNull(_config);

		PlatformService platformService = new(_loggerInstance, _config);

		_serviceCollection
			.AddSingleton(_config)
			.AddSingleton(_loggerInstance)
			.AddSingleton(platformService)
			.AddSingleton<TimeService>()
			.AddSingleton<InputService>()
			.AddSingleton<WindowService>();

		return _serviceCollection;
	}

	#endregion
}
