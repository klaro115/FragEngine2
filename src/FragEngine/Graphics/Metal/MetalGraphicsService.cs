using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using Veldrid;
using Veldrid.Sdl2;

namespace FragEngine.Graphics.Dx11;

/// <summary>
/// Graphics service implementation for the Metal graphics API.
/// </summary>
/// <param name="_logger">The logger service.</param>
internal sealed class MetalGraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	EngineConfig _config)
	: GraphicsService(_logger, _platformService, _windowService, _config)
{
	#region Methods

	internal override bool Initialize(GraphicsServiceInitFlags _initFlags)
	{
		throw new NotImplementedException();
	}

	internal override bool Shutdown()
	{
		throw new NotImplementedException();
	}

	internal override bool Draw()
	{
		throw new NotImplementedException();
	}

	protected override bool HandleSetGraphicsSettings()
	{
		throw new NotImplementedException();
	}

	internal override bool CreateSwapchain(Sdl2Window _window, out Swapchain? _outSwapchain)
	{
		throw new NotImplementedException();
	}

	#endregion
}
