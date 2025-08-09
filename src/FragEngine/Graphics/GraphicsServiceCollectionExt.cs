using FragEngine.EngineCore;
using FragEngine.Extensions;
using FragEngine.Graphics.Dx11;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.Graphics;

/// <summary>
/// Extension methods for adding graphics-related services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class GraphicsServiceCollectionExt
{
	#region Constants

	public const int defaultServiceCount = 1;

	#endregion
	#region Methods

	public static IServiceCollection UseGraphics(this IServiceCollection _serviceCollection)
	{
		ILogger? logger = _serviceCollection.GetLoggerInstance();
		if (logger is null)
		{
			throw new NullReferenceException("Logger instance is missing!");
		}

		EngineConfig? engineConfig = _serviceCollection.GetImplementationInstance<EngineConfig>();
		if (engineConfig is null)
		{
			logger.LogWarning("Service collection does not have an engine configuration! Adding default config now...");

			engineConfig = EngineConfig.CreateDefault();
			_serviceCollection.AddSingleton(engineConfig);
		}

		PlatformService? platformService = _serviceCollection.GetImplementationInstance<PlatformService>();
		if (platformService is null)
		{
			logger.LogWarning("Service collection does not have a platform service implementation! Adding default implementation now...");

			platformService = new(logger, engineConfig);
			_serviceCollection.AddSingleton(platformService);
		}

		if (!AddPlatformSpecficServices(_serviceCollection, platformService, logger))
		{
			throw new Exception("Failed to add graphics service to engine service collection!");
		}

		if (!AddPlatformAgnosticServices(_serviceCollection, platformService, logger))
		{
			throw new Exception("Failed to add graphics service to engine service collection!");
		}

		return _serviceCollection;
	}

	private static bool AddPlatformSpecficServices(IServiceCollection _serviceCollection, PlatformService _platformService, ILogger _logger)
	{
		switch (_platformService.GraphicsBackend)
		{
			case Veldrid.GraphicsBackend.Direct3D11:
				_serviceCollection.AddSingleton<GraphicsService, Dx11GraphicsService>();
				break;
			case Veldrid.GraphicsBackend.Vulkan:
				_serviceCollection.AddSingleton<GraphicsService, VulkanGraphicsService>();
				break;
			case Veldrid.GraphicsBackend.Metal:
				_serviceCollection.AddSingleton<GraphicsService, MetalGraphicsService>();
				break;
			default:
				_logger.LogError($"Cannot add graphics service for unsupported graphics API '{_platformService.GraphicsBackend}'");
				return false;
		}

		return true;
	}

	private static bool AddPlatformAgnosticServices(IServiceCollection _serviceCollection, PlatformService _platformService, ILogger _logger)
	{
		//TODO [later]: Add platform-agnostic services.
		return true;
	}

	#endregion
}
