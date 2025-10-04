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

		try
		{
			SDL_SysWMinfo sysWmInfo = default;

			int errorCode = Sdl2Native.SDL_GetWMWindowInfo(_window.SdlWindowHandle, &sysWmInfo);	// Note: return value is BOOL, not an error code.
			if (errorCode == 0)
			{
				SDL2Helper.GetAndLogError(_logger, "Failed to get Window window manager info for SDL2 window!");
				_outHInstance = -1;
				return false;
			}

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

	#endregion
}
