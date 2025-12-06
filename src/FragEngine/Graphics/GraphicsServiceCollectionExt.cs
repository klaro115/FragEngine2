using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.Extensions;
using FragEngine.Extensions.Veldrid;
using FragEngine.Graphics.Cameras;
using FragEngine.Graphics.Dx11;
using FragEngine.Graphics.Geometry;
using FragEngine.Graphics.Geometry.Export;
using FragEngine.Graphics.Geometry.Import.FMDL;
using FragEngine.Graphics.Metal;
using FragEngine.Graphics.Shaders.Import;
using FragEngine.Graphics.Vulkan;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// Extension methods for adding graphics-related services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class GraphicsServiceCollectionExt
{
	#region Constants

	/// <summary>
	/// The minimum number of graphics services that are added by default. This constant is used as a
	/// reference to check if the service provider contains a realistic number of services for normal
	/// engine operation.
	/// </summary>
	public const int defaultServiceCount = 9;

	#endregion
	#region Methods

	/// <summary>
	/// Adds graphics-related services and configs to a service collection.<para/>
	/// This will add the right <see cref="GraphicsService"/> implementation for the current platform,
	/// as well as all common transient types (ex.: <see cref="Camera"/>) to the engine's service provider.
	/// </summary>
	/// <param name="_serviceCollection">This service collection.</param>
	/// <returns>The service collection, with all graphics services added.</returns>
	/// <exception cref="NullReferenceException">Service collection may not be null, and must include an
	/// implementation of a logging service.</exception>
	/// <exception cref="Exception">Failed to add graphics services to service collection.</exception>
	public static IServiceCollection UseGraphics(this IServiceCollection _serviceCollection)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);

		ILogger? logger = _serviceCollection.GetLoggerInstance() ?? throw new NullReferenceException("Logger instance is missing!");

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
			throw new Exception("Failed to add platform-specific graphics services to service collection!");
		}

		if (!AddPlatformAgnosticServices(_serviceCollection, platformService, logger))
		{
			throw new Exception("Failed to add general graphics services to service collection!");
		}

		return _serviceCollection;
	}

	private static bool AddPlatformSpecficServices(IServiceCollection _serviceCollection, PlatformService _platformService, ILogger _logger)
	{
		// Service count: 1
		if (OperatingSystem.IsWindows() &&
			_platformService.GraphicsBackend == GraphicsBackend.Direct3D11)
		{
			_serviceCollection.AddSingleton<GraphicsService, Dx11GraphicsService>();
		}
		else if ((OperatingSystem.IsIOS() ||
				  OperatingSystem.IsMacOS() ||
				  OperatingSystem.IsMacCatalyst()) &&
				 _platformService.GraphicsBackend == GraphicsBackend.Metal)
		{
			_serviceCollection.AddSingleton<GraphicsService, MetalGraphicsService>();
		}
		else if (!OperatingSystem.IsIOS() &&
				 !OperatingSystem.IsMacOS() &&
				 !OperatingSystem.IsMacCatalyst() &&
				 GraphicsBackend.Vulkan.IsSupportedOnCurrentPlatform() &&
				 _platformService.GraphicsBackend == GraphicsBackend.Vulkan)
		{
			_serviceCollection.AddSingleton<GraphicsService, VulkanGraphicsService>();
		}
		else
		{
			_logger.LogError($"Cannot add graphics service for unsupported graphics API '{_platformService.GraphicsBackend}'");
			return false;
		}

		//... (add further platform-specific services here)

		return true;
	}

	private static bool AddPlatformAgnosticServices(IServiceCollection _serviceCollection, PlatformService _platformService, ILogger _logger)
	{
		// Service count: 8
		_serviceCollection
			// Singletons:
			.AddSingleton<GraphicsResourceService>()
			.AddSingleton<PrimitivesFactory>()
			.AddSingleton<FMdlImporter>()
			.AddSingleton<FMdlExporter>()
			.AddSingleton<SourceCodeShaderImporter>()
			.AddSingleton<GraphicsImportService>()
			// Transient:
			.AddTransient<Camera>()
			.AddTransient<MeshSurface>();

		//... (add further platform-agnostic services here)

		return true;
	}

	#endregion
}
