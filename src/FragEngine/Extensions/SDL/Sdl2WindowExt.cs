using FragEngine.EngineCore.Windows.Linux;
using FragEngine.Helpers;
using FragEngine.Logging;
using System.Runtime.Versioning;
using Veldrid.Sdl2;

namespace FragEngine.Extensions.SDL;

/// <summary>
/// Extension methods for the <see cref="Sdl2Window"/> class.
/// </summary>
public static class Sdl2WindowExt
{
	#region Methods

	/// <summary>
	/// Tries to identify the executing operating system's window manager.
	/// </summary>
	/// <param name="_window">An SDL window from which we want to derive the window manager type.</param>
	/// <param name="_logger">The engine's logger service singleton. This is needed for error output. If null, no errors are logged.</param>
	/// <param name="_outWindowManagerType">Outputs the type of window manager in charge of the given window, or unknown, on failure to identify.</param>
	/// <returns>True if the window manager type could be identified, false otherwise.</returns>
	public static unsafe bool GetWindowManager(this Sdl2Window _window, ILogger? _logger, out SysWMType _outWindowManagerType)
	{
		ArgumentNullException.ThrowIfNull(_window);

		if (!TryGetWindowManagerInfo(_window, _logger, out SDL_SysWMinfo sysWmInfo))
		{
			_logger?.LogError("Failed to get window manager type from SDL window!");
			_outWindowManagerType = SysWMType.Unknown;
			return false;
		}

		_outWindowManagerType = sysWmInfo.subsystem;
		if (_outWindowManagerType == SysWMType.Unknown)
		{
			_logger?.LogError("Failed to identify window manager type!");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Tries to get the HInstance of an SDL2 window.
	/// </summary>
	/// <param name="_window">The SDL window whose app's HInstance we wish to determine.</param>
	/// <param name="_logger">The engine's logger service singleton. This is needed for error output.</param>
	/// <param name="_outHInstance">Outputs the app's instance handle. On error, this will be -1.</param>
	/// <returns>True if the HInstance was determined successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Window and logger may not be null.</exception>
	[SupportedOSPlatform("windows")]
	public static bool TryGetAppHInstance(this Sdl2Window _window, ILogger _logger, out nint _outHInstance)
	{
		ArgumentNullException.ThrowIfNull(_window);
		ArgumentNullException.ThrowIfNull(_logger);

		if (!TryGetWindowManagerInfo(_window, _logger, out SDL_SysWMinfo sysWmInfo))
		{
			_outHInstance = -1;
			return false;
		}

		try
		{
			_outHInstance = sysWmInfo.GetWindowsHInstance();
			return _outHInstance > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException("Failed to get app's HInstance from SDL window.", ex);
			_outHInstance = -1;
			return false;
		}
	}

	/// <summary>
	/// Tries to get Wayland window manager info of an SDL2 window.
	/// </summary>
	/// <param name="_window">The SDL window whose window manager info we wish to determine.</param>
	/// <param name="_logger">The engine's logger service singleton. This is needed for error output.</param>
	/// <param name="_outWaylandInfo">Outputs the window's Wayland info. On error, this will contain all null pointers.</param>
	/// <returns>True if the window manager info was determined successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Window and logger may not be null.</exception>
	[SupportedOSPlatform("linux")]
	public static bool TryGetWaylandInfo(this Sdl2Window _window, ILogger _logger, out LinuxWaylandWMInfo _outWaylandInfo)
	{
		ArgumentNullException.ThrowIfNull(_window);
		ArgumentNullException.ThrowIfNull(_logger);

		if (!TryGetWindowManagerInfo(_window, _logger, out SDL_SysWMinfo sysWmInfo))
		{
			_outWaylandInfo = default;
			return false;
		}

		try
		{
			_outWaylandInfo = sysWmInfo.GetLinuxWaylandInfo();
			return _outWaylandInfo.surface > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException("Failed to get Wayland window manager info from SDL window.", ex);
			_outWaylandInfo = default;
			return false;
		}
	}

	/// <summary>
	/// Tries to get X11 window manager info of an SDL2 window.
	/// </summary>
	/// <param name="_window">The SDL window whose window manager info we wish to determine.</param>
	/// <param name="_logger">The engine's logger service singleton. This is needed for error output.</param>
	/// <param name="_outX11Info">Outputs the window's X11 info. On error, this will contain all null pointers.</param>
	/// <returns>True if the window manager info was determined successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Window and logger may not be null.</exception>
	[SupportedOSPlatform("linux")]
	public static bool TryGetX11Info(this Sdl2Window _window, ILogger _logger, out LinuxX11WMInfo _outX11Info)
	{
		ArgumentNullException.ThrowIfNull(_window);
		ArgumentNullException.ThrowIfNull(_logger);

		if (!TryGetWindowManagerInfo(_window, _logger, out SDL_SysWMinfo sysWmInfo))
		{
			_outX11Info = default;
			return false;
		}

		try
		{
			_outX11Info = sysWmInfo.GetLinuxX11Info();
			return _outX11Info.window > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException("Failed to get X11 window manager info from SDL window.", ex);
			_outX11Info = default;
			return false;
		}
	}

	private static unsafe bool TryGetWindowManagerInfo(Sdl2Window _window, ILogger? _logger, out SDL_SysWMinfo _outWmInfo)
	{
		try
		{
			SDL_SysWMinfo sysWmInfo = default;

			int errorCode = Sdl2Native.SDL_GetWMWindowInfo(_window.SdlWindowHandle, &sysWmInfo);    // Note: return value is BOOL, not an error code.
			if (errorCode == 0)
			{
				if (_logger is not null)
				{
					SDL2Helper.GetAndLogError(_logger, "Failed to get window manager info for SDL2 window!");
				}
				else
				{
					Sdl2Native.SDL_ClearError();
				}
				_outWmInfo = default;
				return false;
			}

			_outWmInfo = sysWmInfo;
			return _outWmInfo.subsystem != SysWMType.Unknown;
		}
		catch (Exception ex)
		{
			_logger?.LogException("Failed to get window manager info from SDL window.", ex);
			_outWmInfo = default;
			return false;
		}
	}

	#endregion
}
