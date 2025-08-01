using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.EngineCore;

public static class EngineServiceCollectionExt
{
	#region Methods

	public static IServiceCollection AddEngine(this IServiceCollection _serviceCollection)
	{
		ConsoleLogger loggerInstance = new();
		return AddEngine(_serviceCollection, loggerInstance, null!);
	}

	public static IServiceCollection AddEngine(this IServiceCollection _serviceCollection, ILogger _loggerInstance)
	{
		PlatformService platformService = new(_loggerInstance);

		_serviceCollection
			.AddSingleton(_loggerInstance)
			.AddSingleton(platformService)
			.AddSingleton<WindowService>();

		return _serviceCollection;
	}

	public static IServiceCollection AddEngine(this IServiceCollection _serviceCollection, ILogger _loggerInstance, EngineConfig _config)
	{
		_serviceCollection
			.AddEngine(_loggerInstance)
			.AddSingleton(_config);

		return _serviceCollection;
	}

	#endregion
}
