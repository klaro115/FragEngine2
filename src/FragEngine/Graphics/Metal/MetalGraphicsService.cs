using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Veldrid;
using Veldrid.Sdl2;

namespace FragEngine.Graphics.Metal;

/// <summary>
/// Graphics service implementation for the Metal graphics API.
/// </summary>
/// <param name="_logger">The logger service.</param>
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("maccatalyst")]
internal sealed class MetalGraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	TimeService _timeService,
	SettingsService _settingsService,
	EngineConfig _config)
	: GraphicsService(_logger, _platformService, _windowService, _timeService, _settingsService, _config)
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

	internal override bool CreateSwapchain(Sdl2Window _window, [NotNullWhen(true)] out Swapchain? _outSwapchain)
	{
		throw new NotImplementedException();
	}

	#endregion
}
