using FragEngine.EngineCore;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;

namespace FragEngine.Graphics.Dx11;

/// <summary>
/// Graphics service implementation for the Vulkan graphics API.
/// </summary>
/// <param name="_logger">The logger service.</param>
internal sealed class VulkanGraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	EngineConfig _config)
	: GraphicsService(_logger, _platformService, _windowService, _config)
{
	#region Methods

	internal override bool Initialize(bool _createMainWindow)
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

	#endregion
}
