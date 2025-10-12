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
	/// Tries to get the HInstance of an SDL2 window.
	/// </summary>
	/// <param name="_window">The SDL window whose app's HInstance we wish to determine.</param>
	/// <param name="_logger">The engine's logger service singleton. This is needed for error output.</param>
	/// <param name="_outHInstance">Outputs the app's instance handle. On error, this will be -1.</param>
	/// <returns>True if the HInstance was determined successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Window and logger may not be null.</exception>
	[SupportedOSPlatform("windows")]
	public static unsafe bool TryGetAppHInstance(this Sdl2Window _window, ILogger _logger, out nint _outHInstance)
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
	/// <param name="_window">The SDL window whose app's HInstance we wish to determine.</param>
	/// <param name="_logger">The engine's logger service singleton. This is needed for error output.</param>
	/// <param name="_outWaylandInfo">Outputs the window's Wayland info. On error, this will contain all null pointers.</param>
	/// <returns>True if the window manager info was determined successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Window and logger may not be null.</exception>
	[SupportedOSPlatform("linux")]
	public static unsafe bool TryGetWaylandInfo(this Sdl2Window _window, ILogger _logger, out LinuxWaylandWMInfo _outWaylandInfo)
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

	private static unsafe bool TryGetWindowManagerInfo(Sdl2Window _window, ILogger _logger, out SDL_SysWMinfo _outWmInfo)
	{
		try
		{
			SDL_SysWMinfo sysWmInfo = default;

			int errorCode = Sdl2Native.SDL_GetWMWindowInfo(_window.SdlWindowHandle, &sysWmInfo);    // Note: return value is BOOL, not an error code.
			if (errorCode == 0)
			{
				SDL2Helper.GetAndLogError(_logger, "Failed to get window manager info for SDL2 window!");
				_outWmInfo = default;
				return false;
			}

			_outWmInfo = sysWmInfo;
			return _outWmInfo.subsystem != SysWMType.Unknown;
		}
		catch (Exception ex)
		{
			_logger.LogException("Failed to get window manager info from SDL window.", ex);
			_outWmInfo = default;
			return false;
		}
	}

	#endregion
}
