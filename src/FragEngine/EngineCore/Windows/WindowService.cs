using FragEngine.Helpers;
using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace FragEngine.EngineCore.Windows;

/// <summary>
/// An engine service handling window management.
/// </summary>
public sealed class WindowService : IExtendedDisposable
{
	#region Events

	/// <summary>
	/// Event that is triggered whenever the window focus changes.
	/// </summary>
	public event FuncWindowFocusChanged? WindowFocusChanged;
	/// <summary>
	/// Event that triggers whenever a new window is created.
	/// </summary>
	public event FuncNewWindowCreated? WindowCreated;
	/// <summary>
	/// Event that triggers whenever a window is closed.
	/// </summary>
	public event FuncWindowClosed? WindowClosed;

	#endregion
	#region Fields

	internal readonly ILogger logger;
	private readonly PlatformService platformService;

	private readonly List<WindowHandle> windows = new(1);
	private readonly SemaphoreSlim windowSemaphore = new(1, 1);

	private int windowIdCounter = 0;

	private WindowHandle? focusedWindow = null;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets the total number of windows.
	/// </summary>
	public int WindowCount => windows.Count;

	/// <summary>
	/// Gets the application window that is currently focused. Null if all windows are closed or unfocused.
	/// </summary>
	public WindowHandle? FocusedWindow
	{
		get => focusedWindow;
		private set
		{
			bool changed = focusedWindow != value;
			focusedWindow = value;
			if (changed)
			{
				WindowFocusChanged?.Invoke(focusedWindow);
			}
		}
	}

	#endregion
	#region Constructors

	public WindowService(ILogger _logger, PlatformService _platformService)
	{
		logger = _logger;
		platformService = _platformService;

		logger.LogStatus("# Initializing window service.");

		const SDLInitFlags initFlags = SDLInitFlags.Audio | SDLInitFlags.Video | SDLInitFlags.GameController;

		int errorCode = 0;
		try
		{
			errorCode = Sdl2Native.SDL_Init(initFlags);
			logger.LogMessage("- SDL initialized.");
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to initialize SDL2!", ex, LogEntrySeverity.Critical);
			Dispose();
		}
		finally
		{
			if (errorCode != 0 && SDL2Helper.GetError(out string errorMessage))
			{
				logger.LogError(errorMessage, LogEntrySeverity.Critical);
			}
		}

		if (!IsDisposed)
		{
			logger.LogMessage("- Window service initialized.");
		}
	}

	~WindowService()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}
	private void Dispose(bool _)
	{
		IsDisposed = true;

		CloseAllWindows();

		windowSemaphore.Dispose();
	}

	/// <summary>
	/// Gets a window by index.
	/// </summary>
	/// <param name="_windowIndex">The index of the window, may not be negative.</param>
	/// <param name="_outHandle">Outputs a handle to the window, or null, if the index was invalid.</param>
	/// <returns>True if an open window exists at that index, false otherwise.</returns>
	public bool GetWindow(int _windowIndex, out WindowHandle? _outHandle)
	{
		if (IsDisposed)
		{
			logger.LogError("Cannot get window from window service that has already been disposed!");
			_outHandle = null;
			return false;
		}

		if (_windowIndex < 0 || _windowIndex >= WindowCount)
		{
			logger.LogError("Window index is out of bounds!");
			_outHandle = null;
			return false;
		}

		windowSemaphore.Wait();

		_outHandle = windows[_windowIndex];
		bool isOpen = _outHandle.IsOpen;

		windowSemaphore.Release();
		return isOpen;
	}

	/// <summary>
	/// Gets a window by ID.
	/// </summary>
	/// <param name="_windowId">The ID number of the window.</param>
	/// <param name="_outHandle">Outputs a handle to the window, or null, if the ID was not found.</param>
	/// <returns>True if an open window exists with that ID, false otherwise.</returns>
	public bool GetWindowByID(uint _windowId, out WindowHandle? _outHandle)
	{
		if (IsDisposed)
		{
			logger.LogError("Cannot get window from window service that has already been disposed!");
			_outHandle = null;
			return false;
		}

		windowSemaphore.Wait();
		
		_outHandle = windows.FirstOrDefault(o => o.WindowId == _windowId);
		bool foundWindow = _outHandle is not null && _outHandle.IsOpen;

		windowSemaphore.Release();
		return foundWindow;
	}

	/// <summary>
	/// Attempts to close all windows of the application.
	/// </summary>
	public void CloseAllWindows()
	{
		windowSemaphore.Wait();

		foreach (WindowHandle handle in windows)
		{
			handle.CloseWindow();
		}
		windows.Clear();

		windowSemaphore.Release();
	}

	/// <summary>
	/// Tries to get the index of the display/screen that a specific point is located on.
	/// </summary>
	/// <param name="_desktopPosition">A screen in desktop pixel space. This is a space that encloses all desktop screens.</param>
	/// <param name="_outScreenIdx">Outputs the index of the screen, or -1, if the position was out-of-bounds of all screens.</param>
	/// <returns>True if the screen index could be determined, false otherwise or if an error occurs.</returns>
	public unsafe bool GetScreenIndex(Vector2 _desktopPosition, out int _outScreenIdx)
	{
		int screenCount = Sdl2Native.SDL_GetNumVideoDisplays();
		if (screenCount <= 0)
		{
			logger.LogWarning("No screens could be detected. Are you trying to create a window in a headless client?", LogEntrySeverity.Trivial);
			_outScreenIdx = -1;
			return false;
		}

		Rectangle screenDesktopBounds = new();

		for (int i = 0; i < screenCount; i++)
		{
			int errorCode = Sdl2Native.SDL_GetDisplayBounds(i, &screenDesktopBounds);
			if (SDL2Helper.CheckAndLogError(logger, errorCode, $"Failed to get bounds of screen {i}!", true))
			{
				break;
			}

			if (screenDesktopBounds.Contains((int)_desktopPosition.X, (int)_desktopPosition.Y))
			{
				_outScreenIdx = i;
				return true;
			}
		}

		_outScreenIdx = -1;
		return false;
	}

	/// <summary>
	/// Tries to get the position, resolution, and other metrics of a screen.
	/// </summary>
	/// <param name="_screenIdx">The ID of the screen.</param>
	/// <param name="_outDesktopPosition">Outputs the position in desktop space.
	/// Your screen may not be located at (0,0) if you have a multi-monitor setup.</param>
	/// <param name="_outResolution">Outputs the resolution of the screen, in pixels.</param>
	/// <param name="_outRefreshRate">Outputs the refresh rate of the monitor.</param>
	/// <returns>True if the screen exists and could be measured, false otherwise.</returns>
	public unsafe bool GetScreenMetrics(int _screenIdx, out Vector2 _outDesktopPosition, out Vector2 _outResolution, out float _outRefreshRate)
	{
		_outDesktopPosition = Vector2.Zero;
		_outResolution = Vector2.Zero;
		_outRefreshRate = 60;

		try
		{
			Rectangle screenDesktopBounds = new();
			int errorCode = Sdl2Native.SDL_GetDisplayBounds(_screenIdx, &screenDesktopBounds);
			if (SDL2Helper.CheckAndLogError(logger, errorCode, $"Failed to get bounds of screen {_screenIdx}!", true))
			{
				return false;
			}

			_outDesktopPosition = screenDesktopBounds.Position;
			_outResolution = screenDesktopBounds.Size;

			SDL_DisplayMode displayMode = default;
			errorCode = Sdl2Native.SDL_GetDesktopDisplayMode(_screenIdx, &displayMode);
			if (SDL2Helper.CheckAndLogError(logger, errorCode, $"Failed to get display mode of screen {_screenIdx}!", true))
			{
				return false;
			}

			_outRefreshRate = displayMode.refresh_rate;
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to get metrics for screen index {_screenIdx}!", ex, LogEntrySeverity.Normal);
			return false;
		}
	}

	/// <summary>
	/// Creates a new window.
	/// </summary>
	/// <remarks>
	/// Note that this creates an empty window without any swapchain or graphics context.
	/// </remarks>
	/// <param name="_title">The title of the window.</param>
	/// <param name="_position">The position of the window, in desktop space.</param>
	/// <param name="_resolution">The size of the window, in pixels.</param>
	/// <param name="_isFullscreen">Whether this is a fullscreen window.</param>
	/// <param name="_outHandle">Outputs a handle for the newly created windows, or null on failure.</param>
	/// <returns>True if a new window was created, false otherwise.</returns>
	public bool CreateWindow(string _title, Vector2 _position, Vector2 _resolution, bool _isFullscreen, out WindowHandle? _outHandle)
	{
		if (string.IsNullOrEmpty(_title))
		{
			_title = string.Empty;
		}

		// Determine window flags:
		const SDL_WindowFlags baseFlags = SDL_WindowFlags.Shown | SDL_WindowFlags.InputFocus | SDL_WindowFlags.AllowHighDpi;

		SDL_WindowFlags flags = baseFlags;
		if (_isFullscreen)
		{
			const SDL_WindowFlags fullscreenFlags = SDL_WindowFlags.Fullscreen;
			const SDL_WindowFlags desktopFullscreenFlags = SDL_WindowFlags.FullScreenDesktop | SDL_WindowFlags.Borderless;
			flags |= fullscreenFlags;

			if (!GetScreenIndex(_position, out int screenIdx))
			{
				_outHandle = null;
				return false;
			}

			if (!GetScreenMetrics(screenIdx, out Vector2 screenPosition, out Vector2 screenResolution, out _))
			{
				_outHandle = null;
				return false;
			}

			if (Vector2.Round(_resolution) == Vector2.Round(screenResolution))
			{
				_position = screenPosition;
				flags |= desktopFullscreenFlags;
			}
		}
		else
		{
			const SDL_WindowFlags dynamicWindowFlags = SDL_WindowFlags.Resizable;
			flags |= dynamicWindowFlags;
		}

		// Add platform-specific flags for graphics APIs:
		switch (platformService.GraphicsBackend)
		{
			case GraphicsBackend.OpenGL:
			case GraphicsBackend.OpenGLES:
				flags |= SDL_WindowFlags.OpenGL;
				break;
			default:
				break;
		}

		// Create the window:
		Sdl2Window window;
		try
		{
			window = new(_title, (int)_position.X, (int)_position.Y, (int)_resolution.X, (int)_resolution.Y, flags, true);
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create window! (Title: '{_title}')", ex);
			_outHandle = null;
			return false;
		}

		// Register window:
		bool wasAdded = AddWindow(window, out _outHandle);
		if (!wasAdded)
		{
			window.Close();
			_outHandle = null;
			return false;
		}

		return true;
	}

	public bool AddWindow(Sdl2Window _newWindow, out WindowHandle? _outHandle)
	{
		if (_newWindow is null || !_newWindow.Exists)
		{
			logger.LogError("Cannot register null or non-existent window!");
			_outHandle = null;
			return false;
		}

		int windowId = windowIdCounter++;
		_outHandle = new(this, logger, _newWindow, windowId);

		windowSemaphore.Wait();
		windows.Add(_outHandle);
		windowSemaphore.Release();

		WindowCreated?.Invoke(_outHandle);
		return true;
	}

	internal bool RemoveWindow(WindowHandle _handle)
	{
		if (IsDisposed)
		{
			return false;
		}
		if (_handle?.Window is null)
		{
			logger.LogError("Cannot unregister null window!");
			return false;
		}

		windowSemaphore.Wait();
		bool removed = windows.Remove(_handle);
		windowSemaphore.Release();

		WindowClosed?.Invoke(_handle);
		return removed;
	}

	/// <summary>
	/// Updates all window states, processes events, and generates input data from the currently focused window.
	/// </summary>
	/// <param name="_outInputSnapshot">Outputs a snapshot of all input events since last frame.
	/// Null if no window is open, or if no application window is currently focused.</param>
	/// <returns>True if windows were updated successfully, false on error.</returns>
	internal bool Update(out InputSnapshot? _outInputSnapshot)
	{
		if (IsDisposed)
		{
			logger.LogError($"Cannot update windows, {nameof(WindowService)} has already been disposed!");
			_outInputSnapshot = null;
			return false;
		}

		_outInputSnapshot = null;

		windowSemaphore.Wait();
		
		try
		{
			foreach (WindowHandle handle in windows)
			{
				if (handle.Window.Focused)
				{
					FocusedWindow = handle;
					_outInputSnapshot = handle.Window.PumpEvents();
				}
				else
				{
					handle.Window.PumpEvents(null!);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to pump events and update windows!", ex, LogEntrySeverity.High);
			return false;
		}
		finally
		{
			windowSemaphore.Release();
		}
	}

	#endregion
}
