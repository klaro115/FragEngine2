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
	#region Fields

	internal readonly ILogger logger;

	private readonly List<WindowHandle> windows = new(1);

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	public WindowService(ILogger _logger)
	{
		logger = _logger;

		const SDLInitFlags initFlags = SDLInitFlags.GameController | SDLInitFlags.Audio;

		int errorCode = 0;
		try
		{
			errorCode = Sdl2Native.SDL_Init(initFlags);
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
	}

	/// <summary>
	/// Attempts to close all windows of the application.
	/// </summary>
	public void CloseAllWindows()
	{
		foreach (WindowHandle handle in windows)
		{
			handle.CloseWindow();
		}
		windows.Clear();
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

	public unsafe bool GetScreenMetrics(int _screenIdx, out Vector2 _outDesktopPosition, out Vector2 _outResolution, out float _outRefreshRate)
	{
		_outDesktopPosition = Vector2.Zero;
		_outResolution = Vector2.Zero;
		_outRefreshRate = 60;

		try
		{
			Rectangle screenDesktopBounds = new();
			int errorCode = Sdl2Native.SDL_GetDisplayBounds(_screenIdx, &screenDesktopBounds);
			if (errorCode != 0 && SDL2Helper.GetError(out string errorMessage))
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

		//TODO: Add platform-specific flags for graphics APIs!

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

		_outHandle = new(this, logger, _newWindow);

		windows.Add(_outHandle);
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

		bool removed = windows.Remove(_handle);
		return removed;
	}

	#endregion
}
