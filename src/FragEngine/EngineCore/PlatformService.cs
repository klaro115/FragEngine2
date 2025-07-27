using FragEngine.EngineCore.Enums;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.EngineCore;

/// <summary>
/// Engine service for managing the current OS platform and APIs.
/// </summary>
public sealed class PlatformService
{
	#region Fields

	private readonly ILogger logger;

	#endregion
	#region Properties

	/// <summary>
	/// Gets operating system that the engine is currently running on.
	/// </summary>
	public OperatingSystemType OperatingSystem { get; }
	/// <summary>
	/// Gets the graphics API that the engine is currently running on.
	/// </summary>
	public GraphicsBackend GraphicsBackend { get; }

	#endregion
	#region Constructors

	public PlatformService(ILogger _logger)
	{
		logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

		// DESKTOP PLATFORMS:
		if (System.OperatingSystem.IsWindows())
		{
			OperatingSystem = OperatingSystemType.Window;
			GraphicsBackend = GraphicsBackend.Direct3D11;	//TODO: Use Vulkan instead if non-native API flag is set in launch settings.
		}
		else if (System.OperatingSystem.IsLinux())
		{
			OperatingSystem = OperatingSystemType.Linux;
			GraphicsBackend = GraphicsBackend.Vulkan;
		}
		else if (System.OperatingSystem.IsMacOS() || System.OperatingSystem.IsMacCatalyst())
		{
			OperatingSystem = OperatingSystemType.MacOS;
			GraphicsBackend = GraphicsBackend.Metal;
		}
		else if (System.OperatingSystem.IsFreeBSD())
		{
			OperatingSystem = OperatingSystemType.BSD;
			GraphicsBackend = GraphicsBackend.Vulkan;
		}

		// MOBILE PLATFORMS:
		else if (System.OperatingSystem.IsIOS())
		{
			OperatingSystem = OperatingSystemType.iOS;
			GraphicsBackend = GraphicsBackend.Metal;
		}
		else if (System.OperatingSystem.IsAndroid())
		{
			OperatingSystem = OperatingSystemType.Android;
			GraphicsBackend = GraphicsBackend.Vulkan;
		}
		else
		{
			throw new PlatformNotSupportedException("Operating system is not supported!");
		}
	}

	#endregion
	#region Methods

	//...

	#endregion
}
