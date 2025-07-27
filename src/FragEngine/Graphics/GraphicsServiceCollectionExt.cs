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
	#region Methods

	public static ServiceCollection AddGraphics(this ServiceCollection _serviceCollection)
	{
		ILogger? logger = _serviceCollection.GetLoggerInstance();
		if (logger is null)
		{
			throw new NullReferenceException("Logger instance is missing!");
		}

		PlatformService? platformService = _serviceCollection.GetImplementationInstance<PlatformService>();
		if (platformService is null)
		{
			logger.LogWarning("Service collection does not have a platform service implementation! Adding default implementation now...");

			platformService = new(logger);
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

	private static bool AddPlatformSpecficServices(ServiceCollection _serviceCollection, PlatformService _platformService, ILogger _logger)
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

	private static bool AddPlatformAgnosticServices(ServiceCollection _serviceCollection, PlatformService _platformService, ILogger _logger)
	{
		//TODO [later]: Add platform-agnostic services.
		return true;
	}

	#endregion
}
