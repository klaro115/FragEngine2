using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class IServiceCollectionExt
{
	#region Methods

	/// <summary>
	/// Gets the logging service instance's implementation instance.
	/// </summary>
	/// <param name="_services">This service collection.</param>
	/// <returns>The logger instance.</returns>
	public static ILogger? GetLoggerInstance(this IServiceCollection _services)
	{
		ArgumentNullException.ThrowIfNull(_services);

		ILogger? logger = _services.FirstOrDefault(o => o.ServiceType.IsAssignableTo(typeof(ILogger)))?.ImplementationInstance as ILogger;
		return logger;
	}

	/// <summary>
	/// Gets implementation instance of a service.
	/// </summary>
	/// <typeparam name="TService">The type of the service.</typeparam>
	/// <param name="_services">This service collection.</param>
	/// <returns>The service's implementation instance, or null, if the service wasn't found or doesn't have an existing instance.</returns>
	public static TService? GetImplementationInstance<TService>(this IServiceCollection _services) where TService : class
	{
		ArgumentNullException.ThrowIfNull(_services);

		TService? service = _services.FirstOrDefault(o => o.ServiceType == typeof(TService))?.ImplementationInstance as TService;
		return service;
	}

	#endregion
}
