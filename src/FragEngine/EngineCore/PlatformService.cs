using FragEngine.EngineCore.Enums;
using FragEngine.Extensions;
using FragEngine.Logging;
using System.Reflection;
using Veldrid;

namespace FragEngine.EngineCore;

/// <summary>
/// Engine service for managing the current OS platform and APIs.
/// </summary>
public sealed class PlatformService
{
	#region Fields

	private readonly ILogger logger;
	private readonly EngineConfig config;

	public readonly string rootDirectoryPath = string.Empty;
	public readonly string settingsDirectoryPath = string.Empty;

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

	public PlatformService(ILogger _logger, EngineConfig _config)
	{
		logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
		config = _config ?? throw new ArgumentNullException(nameof(_config));

		logger.LogStatus("# Initializing platform service.");

		// Platform & API:
		if (!DetermineOperatingSystem(out OperatingSystemType operatingSystem, out GraphicsBackend graphicsBackend))
		{
			throw new PlatformNotSupportedException("Operating system is not supported!");
		}
		OperatingSystem = operatingSystem;
		GraphicsBackend = graphicsBackend;

		logger.LogMessage($"- Operating system: {OperatingSystem}");
		logger.LogMessage($"- Graphics API: {GraphicsBackend}");

		// Application paths:
		rootDirectoryPath = GetRootDirectoryPath();
		settingsDirectoryPath = Path.Combine(rootDirectoryPath, "settings" + Path.DirectorySeparatorChar);

		if (!CreateRootDirectories())
		{
			throw new Exception("Failed to prepare root directories!");
		}

		string truncatedDirectoryPath = rootDirectoryPath.TruncateWithEllipsis(64, "...", StringExt.TruncationType.Start);
		logger.LogMessage($"- Root directory: {truncatedDirectoryPath}");
	}

	#endregion
	#region Methods

	private bool DetermineOperatingSystem(out OperatingSystemType _outOperatingSystem, out GraphicsBackend _outGraphicsBackend)
	{
		// DESKTOP PLATFORMS:
		if (System.OperatingSystem.IsWindows())
		{
			_outOperatingSystem = OperatingSystemType.Window;
			_outGraphicsBackend = config.Graphics.PreferNativeGraphicsAPI
				? GraphicsBackend.Direct3D11
				: GraphicsBackend.Vulkan;
		}
		else if (System.OperatingSystem.IsLinux())
		{
			_outOperatingSystem = OperatingSystemType.Linux;
			_outGraphicsBackend = GraphicsBackend.Vulkan;
		}
		else if (System.OperatingSystem.IsMacOS() || System.OperatingSystem.IsMacCatalyst())
		{
			_outOperatingSystem = OperatingSystemType.MacOS;
			_outGraphicsBackend = GraphicsBackend.Metal;
		}
		else if (System.OperatingSystem.IsFreeBSD())
		{
			_outOperatingSystem = OperatingSystemType.BSD;
			_outGraphicsBackend = GraphicsBackend.Vulkan;
		}

		// MOBILE PLATFORMS:
		else if (System.OperatingSystem.IsIOS())
		{
			_outOperatingSystem = OperatingSystemType.iOS;
			_outGraphicsBackend = GraphicsBackend.Metal;
		}
		else if (System.OperatingSystem.IsAndroid())
		{
			_outOperatingSystem = OperatingSystemType.Android;
			_outGraphicsBackend = GraphicsBackend.Vulkan;
		}

		// OTHER:
		else
		{
			_outOperatingSystem = OperatingSystemType.Unknown;
			_outGraphicsBackend = (GraphicsBackend)255;
			return false;
		}

		return true;
	}

	private bool CreateRootDirectories()
	{
		if (!Directory.Exists(rootDirectoryPath))
		{
			logger.LogError("Application root directory does not exist!");
			return false;
		}

		try
		{
			if (!Directory.Exists(settingsDirectoryPath))
			{
				Directory.CreateDirectory(settingsDirectoryPath);
			}
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create settings directory!", ex, LogEntrySeverity.Critical);
			return false;
		}
	}

	/// <summary>
	/// Gets the root directory path of the application.
	/// This is where the executable and all root folders for assets and settings should be.
	/// </summary>
	/// <returns>A directory path.</returns>
	public static string GetRootDirectoryPath()
	{
		string? rootDirPath = Assembly.GetEntryAssembly()?.Location;
		if (string.IsNullOrEmpty(rootDirPath))
		{
			rootDirPath = typeof(PlatformService).Assembly.Location;
		}
		string rootDirectoryPath = Path.GetDirectoryName(rootDirPath)!;
		return rootDirectoryPath;
	}

	#endregion
}
