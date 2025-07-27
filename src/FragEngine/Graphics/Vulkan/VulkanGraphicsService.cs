using FragEngine.EngineCore.Windows;
using FragEngine.Logging;

namespace FragEngine.Graphics.Dx11;

/// <summary>
/// Graphics service implementation for the Vulkan graphics API.
/// </summary>
/// <param name="_logger">The logger service.</param>
internal sealed class VulkanGraphicsService(ILogger _logger, WindowService _windowService) : GraphicsService(_logger, _windowService)
{
	#region Methods

	internal override bool Initialize()
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

	#endregion
}
