using FragEngine.EngineCore;
using FragEngine.EngineCore.Windows;
using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// Engine service that manages the graphics device and main window swapchain.
/// Draw calls are written to command lists and committed to this service.
/// Committed lists are executed and the output rendered to screen once per update cycle.
/// </summary>
/// <param name="_logger">The engine's logging service.</param>
/// <param name="_windowService">The engine's window management service.</param>
public abstract class GraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	EngineConfig _config) : IExtendedDisposable
{
	#region Events

	/// <summary>
	/// Event that is triggered whenever the graphics settings are about to change.
	/// </summary>
	public event FuncGraphicsSettingsChanging? GraphicsSettingsChanging;
	/// <summary>
	/// Event that is triggered whenever the graphics settings have changed.
	/// </summary>
	public event FuncGraphicsSettingsChanged? GraphicsSettingsChanged;

	#endregion
	#region Fields

	protected readonly ILogger logger = _logger;
	protected readonly PlatformService platformService = _platformService;
	protected readonly WindowService windowService = _windowService;
	protected readonly GraphicsConfig config = _config.Graphics;

	private bool isInitialized = false;

	private GraphicsSettings? settings = null;
	protected ConcurrentQueue<CommandList> committedCommandLists = new();

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets whether the graphics system is initialized. This always be true inside the engine's main loop states.
	/// </summary>
	public bool IsInitialized
	{
		get => !IsDisposed && isInitialized;
		protected set => isInitialized = !IsDisposed && value;
	}

	/// <summary>
	/// Gets the main graphics device.
	/// </summary>
	public GraphicsDevice Device { get; protected set; } = null!;
	/// <summary>
	/// Gets a handle for the the main window. If null, there may not be a main window.
	/// </summary>
	public WindowHandle? MainWindow { get; protected set; } = null;
	/// <summary>
	/// Gets the graphics device's main resource factory.
	/// </summary>
	public ResourceFactory ResourceFactory { get; protected set; } = null!;

	/// <summary>
	/// Gets or sets the current graphics settings.
	/// </summary>
	protected GraphicsSettings Settings
	{
		get => settings ??= GetGraphicsSettings();
		set => SetGraphicsSettings(value);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	protected virtual void Dispose(bool _disposing)
	{
		IsDisposed = true;

		MainWindow?.Dispose();
		Device?.Dispose();
	}

	/// <summary>
	/// Initializes the graphics device, and optionally creates a swapchain and main window.
	/// </summary>
	/// <param name="_createMainWindow">Whether to create the main window and swapchain immediately.</param>
	/// <returns>True on success, false otherwise.</returns>
	internal abstract bool Initialize(bool _createMainWindow);

	/// <summary>
	/// Shuts down the graphics device and releases core resources.
	/// </summary>
	/// <returns>True if the service shut down without errors.</returns>
	internal abstract bool Shutdown();

	/// <summary>
	/// Execute all drawcalls from across all committed command lists.
	/// </summary>
	/// <returns>True if draw calls were executed successfully, false otherwise.</returns>
	internal abstract bool Draw();

	/// <summary>
	/// Submits a command list with GPU instructions for rendering.
	/// The command list will be added to a queue, and executed during the main loop's draw stage.
	/// </summary>
	/// <param name="_cmdList">A command list. Ownership of this instance remains with the caller.</param>
	/// <returns>True if the command list was enqueued for execution during the upcoming frame, false otherwise.</returns>
	public bool CommitCommandList(CommandList _cmdList)
	{
		if (!IsInitialized)
		{
			logger.LogError("Cannot commit command list through uninitialized graphics service!");
			return false;
		}
		if (_cmdList.IsDisposed)
		{
			logger.LogError("Cannot commit command list that has already been disposed!");
			return false;
		}

		committedCommandLists.Enqueue(_cmdList);
		return true;
	}

	/// <summary>
	/// Gets the current graphics settings, or reloads them from file.
	/// </summary>
	/// <returns>The currently applicable graphics settings.</returns>
	public GraphicsSettings GetGraphicsSettings()
	{
		if (settings is not null)
		{
			return settings;
		}

		ReloadGraphicsSettingsFromFile();

		return settings!;
	}

	/// <summary>
	/// Assigns new graphics settings. Some changes may cause screen flickering or visual artifacts.
	/// Ensure your graphics systems can handle all device changes that may occur by the changes made.
	/// </summary>
	/// <param name="_newSettings">The new graphics settings.</param>
	/// <returns>True if graphics settings were changed successfully, false otherwise.</returns>
	public bool SetGraphicsSettings(GraphicsSettings _newSettings)
	{
		if (_newSettings is null)
		{
			logger.LogError("Cannot assign null graphics settings!");
			return false;
		}
		if (!_newSettings.IsValid())
		{
			logger.LogError("Cannot assign invalid graphics settings!");
			return false;
		}

		GraphicsSettings? previousSettings = settings;
		GraphicsSettingsChanging?.Invoke(previousSettings, _newSettings);

		settings = _newSettings;

		if (!HandleSetGraphicsSettings())
		{
			logger.LogError("Failed to apply new graphics settings!", LogEntrySeverity.High);
			return false;
		}

		GraphicsSettingsChanged?.Invoke(previousSettings, settings);
		return true;
	}

	/// <summary>
	/// Request graphics settings to be loaded anew from JSON file.
	/// </summary>
	/// <returns></returns>
	public bool ReloadGraphicsSettingsFromFile()
	{
		string settingsPath = Path.Combine(platformService.settingsDirectoryPath, "graphics.json");
		GraphicsSettings? newSettings = null;

		// If settings file does not exit, use default values:
		if (!File.Exists(settingsPath))
		{
			logger.LogWarning("Graphics settings JSON does not exit, using default values instead.", LogEntrySeverity.Trivial);
			goto applySettings;
		}

		// Load settings from file:
		try
		{
			string settingsJson = File.ReadAllText(settingsPath);
			newSettings = JsonSerializer.Deserialize<GraphicsSettings>(settingsJson);
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to load graphics settings from file, using default values instead.", ex);
			goto applySettings;
		}
		
		if (newSettings is null)
		{
			logger.LogWarning("Engine config loaded from file was null, using default values instead.", LogEntrySeverity.Trivial);
		}

	applySettings:
		newSettings ??= GraphicsSettings.CreateDefaultForConfig(config);
		return SetGraphicsSettings(newSettings!);
	}

	protected abstract bool HandleSetGraphicsSettings();

	protected bool GetWindowSettings(out string _outWindowTitle, out Vector2 _outWindowPosition, out Vector2 _outWindowSize)
	{
		int screenIndex = Settings.OutputScreenIndex >= 0 ? Settings.OutputScreenIndex : (int)config.MainWindowScreenIndex;

		if (!windowService.GetScreenMetrics(screenIndex, out Vector2 screenPosition, out Vector2 screenResolution, out _))
		{
			if (!windowService.GetScreenMetrics(0, out screenPosition, out screenResolution, out _))
			{
				logger.LogError("Failed to get screen metrics, can't center window on screen!");
				screenPosition = Vector2.Zero;
				screenResolution = new Vector2(1920, 1080);
			}
		}

		_outWindowTitle = !string.IsNullOrEmpty(config.MainWindowTitle) ? config.MainWindowTitle : "Fragment Engine";
		_outWindowSize = Settings.OutputResolution ?? Vector2.Min(screenResolution, new Vector2(1920, 1080));
		_outWindowPosition = screenPosition + screenResolution / 2 - _outWindowSize / 2;
		return true;
	}

	#endregion
}
