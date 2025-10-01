using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Contexts;
using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Veldrid;
using Veldrid.Sdl2;

namespace FragEngine.Graphics;

/// <summary>
/// Engine service that manages the graphics device and main window swapchain.
/// Draw calls are written to command lists and committed to this service.
/// Committed lists are executed and the output rendered to screen once per update cycle.
/// </summary>
/// <param name="_logger">The engine's logging service.</param>
/// <param name="_windowService">The engine's window management service.</param>
/// <param name="_platformService">The engine's platform info service.</param>
/// <param name="_timeService">The engine's time management service.</param>
/// <param name="_config">The main engine configuration.</param>
public abstract class GraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	TimeService _timeService,
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

	/// <summary>
	/// Event that is triggered immediately after the buffers of the <see cref="MainSwapchain"/> are swapped.
	/// </summary>
	public event FuncMainSwapchainSwapped? MainSwapchainSwapped;

	#endregion
	#region Fields

	protected readonly ILogger logger = _logger;
	protected readonly PlatformService platformService = _platformService;
	protected readonly WindowService windowService = _windowService;
	protected readonly TimeService timeService = _timeService;
	protected readonly GraphicsConfig config = _config.Graphics;

	private bool isInitialized = false;

	private GraphicsSettings? settings = null;

	protected ConcurrentQueue<(CommandList CmdList, int Priority)> committedCommandLists = new();
	protected PriorityQueue<CommandList, int> commandListExecutionQueue = new();
	protected readonly ReaderWriterLockSlim commandListLock = new(LockRecursionPolicy.NoRecursion);   // Warning; Read/write is reversed here! Read=commit, Write=execution

	protected CBGraphics cbGraphicsData = CBGraphics.Default;
	protected DeviceBuffer? bufCbGraphics = null;

	#endregion
	#region Constants

	private const int commandListLockCommitTimeoutMs = 500;
	private const int commandListLockExecutionTimeoutMs = 1000;

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
	/// Gets whether the graphics system is currently processing and executing draw calls. Don't commit any new command list while this is true.
	/// </summary>
	public bool IsExecuting => commandListLock.IsWriteLockHeld;

	/// <summary>
	/// Gets the main graphics device.
	/// </summary>
	public GraphicsDevice Device { get; protected set; } = null!;
	/// <summary>
	/// Gets a handle for the the main window. If null, there may not be a main window.
	/// </summary>
	public WindowHandle? MainWindow { get; protected set; } = null;
	/// <summary>
	/// Gets the graphics device's main swapchain. If null, there may not be a main swapchain or main window.
	/// </summary>
	public Swapchain? MainSwapchain { get; protected set; } = null;
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

		bufCbGraphics?.Dispose();

		MainWindow?.Dispose();
		Device?.Dispose();

		commandListLock?.Dispose();
	}

	/// <summary>
	/// Initializes the graphics device, and optionally creates a swapchain and main window.
	/// </summary>
	/// <param name="_initFlags">Bit flags indicating which features should be initialized immediately.</param>
	/// <returns>True on success, false otherwise.</returns>
	internal abstract bool Initialize(GraphicsServiceInitFlags _initFlags);

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
	/// <param name="_priority">A custom priority rating; command lists with a lower value are executed first,
	/// lists with the same priority are executed in order of committment.</param>
	/// <returns>True if the command list was enqueued for execution during the upcoming frame, false otherwise.</returns>
	public bool CommitCommandList(CommandList _cmdList, int _priority = 100)
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

		// Acquire a read lock, aka spin-wait until write lock (execution) is released. Abort on timeout:
		if (!commandListLock.TryEnterReadLock(commandListLockCommitTimeoutMs))
		{
			logger.LogError("Cannot commit command list; timeout due to ongoing command list execution lock!");
			return false;
		}

		committedCommandLists.Enqueue((CmdList: _cmdList, Priority: _priority));
		commandListLock.ExitReadLock();
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

		// Check if graphics settings are actually changing, quietly return if they haven't:
		if (settings is not null && _newSettings.Checksum == settings.Checksum)
		{
			return true;
		}

		// Assign new settings:
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

	/// <summary>
	/// Perform platform-specific logic after graphics settings have changed.
	/// </summary>
	/// <returns>True if settings were applied and processed successfully, false otherwise.</returns>
	protected abstract bool HandleSetGraphicsSettings();

	/// <summary>
	/// Get various settings and metrics for creating the main window.
	/// </summary>
	/// <param name="_outWindowTitle">Outputs the title of the main window.</param>
	/// <param name="_outWindowPosition">Outputs the position of the window, in desktop space, measured in pixels.</param>
	/// <param name="_outWindowSize">Outputs the size of the window, in pixels.</param>
	/// <returns>True if main window settings could be determined, false otherwise.</returns>
	protected bool GetWindowSettings(out string _outWindowTitle, out Vector2 _outWindowPosition, out Vector2 _outWindowSize)
	{
		int screenIndex = Settings.OutputScreenIndex >= 0 ? Settings.OutputScreenIndex : (int)config.MainWindowScreenIndex;

		if (!windowService.GetScreenMetrics(screenIndex, out Vector2 screenPosition, out Vector2 screenResolution, out _))
		{
			logger.LogWarning($"Screen with index {screenIndex} could not be found, trying main screen instead.", LogEntrySeverity.Trivial);

			if (!windowService.GetScreenMetrics(0, out screenPosition, out screenResolution, out _))
			{
				logger.LogError("Failed to get screen metrics, can't center window on screen!");
				screenPosition = Vector2.Zero;
				screenResolution = new Vector2(1920, 1080);
			}
		}

		_outWindowTitle = !string.IsNullOrEmpty(config.MainWindowTitle) ? config.MainWindowTitle : EngineConstants.engineDisplayName;
		_outWindowSize = Settings.OutputResolution ?? Vector2.Min(screenResolution, new Vector2(1920, 1080));
		_outWindowPosition = screenPosition + screenResolution / 2 - _outWindowSize / 2;
		return true;
	}

	/// <summary>
	/// Create a new swapchain around an existing SDL window.
	/// </summary>
	/// <param name="_window">The SDL window to create a swapchain for.</param>
	/// <param name="_outSwapchain">Output the new swapchain, or null, on failure.</param>
	/// <returns>True if a swapchain could be created, false otherwise.</returns>
	internal abstract bool CreateSwapchain(Sdl2Window _window, [NotNullWhen(true)] out Swapchain? _outSwapchain);

	/// <summary>
	/// Moves all committed command lists to execution queue, and sorts them by priority.
	/// </summary>
	/// <returns>True if command lists were prepared for execution, false otherwise.</returns>
	protected bool PrepareCommandListExecution()
	{
		if (!commandListLock.TryEnterWriteLock(commandListLockExecutionTimeoutMs))
		{
			logger.LogError("Cannot prepare command list execution; timeout due to ongoing commit locks!");
			return false;
		}

		commandListExecutionQueue.Clear();

		while (committedCommandLists.TryDequeue(out var commit))
		{
			commandListExecutionQueue.Enqueue(commit.CmdList, commit.Priority);
		}

		committedCommandLists.Clear();
		commandListLock.ExitWriteLock();
		return true;
	}

	/// <summary>
	/// Triggers the '<see cref="MainSwapchainSwapped"/>' event.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void OnMainSwapchainSwapped()
	{
		if (MainWindow is not null && MainWindow.IsOpen)
		{
			MainSwapchainSwapped?.Invoke(MainWindow);
		}
	}

	/// <summary>
	/// Begins a new frame. Call this before committing any command lists.
	/// </summary>
	/// <param name="_firstCmdList">The command list that will be committed and executed first during the upcoming frame.</param>
	/// <param name="_outGraphicsCtx">Output a graphics context object for the upcoming frame. Null on error.<para/>
	/// WARNING: This is a transient instance that is only valid for one frame! Do not keep any references to this object or
	/// the resources referenced therein, as they may be subject to change in-between frames.</param>
	/// <returns>True if the frame was started successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Command list may not be null.</exception>
	public virtual bool BeginFrame(CommandList _firstCmdList, [NotNullWhen(true)] out GraphicsContext? _outGraphicsCtx)
	{
		ArgumentNullException.ThrowIfNull(_firstCmdList);

		if (IsDisposed)
		{
			logger.LogError("Cannot begin frame on disposed graphics service!", LogEntrySeverity.High);
			_outGraphicsCtx = null;
			return false;
		}

		// Update and upload graphics constant buffer:
		if (bufCbGraphics is null)
		{
			bufCbGraphics = ResourceFactory.CreateBuffer(CBGraphics.BufferDesc);
			bufCbGraphics.Name = "BufCbGraphics";
		}
		cbGraphicsData.UpdateEngineData(in timeService, in windowService);

		_firstCmdList.UpdateBuffer(bufCbGraphics, 0, ref cbGraphicsData);

		// Assemble context object:
		_outGraphicsCtx = new()
		{
			Graphics = this,
			CbGraphics = cbGraphicsData,
			BufCbGraphics = bufCbGraphics,
			//...
		};
		return true;
	}

	#endregion
}
