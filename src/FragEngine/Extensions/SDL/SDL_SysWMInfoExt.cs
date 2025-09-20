using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Veldrid.Sdl2;

namespace FragEngine.Extensions.SDL;

/// <summary>
/// Extension methods for the <see cref="SDL_SysWMinfo"/> union struct.
/// </summary>
public static class SDL_SysWMInfoExt
{
	#region Types

	[SupportedOSPlatform("windows")]
	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = maximumByteSize)]
	private readonly record struct WindowsWMInfo
	{
		public readonly nint hWnd;
		public readonly nint hDC;
		public readonly nint hInstance;

		public const int maximumByteSize = 64;
	}

	#endregion
	#region Methods

	/// <summary>
	/// Gets the HInstance of an SDL window.
	/// </summary>
	/// <remarks>
	/// This method is available on Windows platforms only.
	/// </remarks>
	/// <param name="_sysWmInfo">Window manager info for an SDL window.</param>
	/// <returns>The HInstance of the app that created the window, or -1, if the instance handle is not known.</returns>
	/// <exception cref="PlatformNotSupportedException">Method was called on a non-windows platform.</exception>
	/// <exception cref="Exception">Failed to get HInstance from windows-specific blob in info union. This is likely a pointer or casting error.</exception>
	[SupportedOSPlatform("windows")]
	public static unsafe nint GetWindowsHInstance(this SDL_SysWMinfo _sysWmInfo)
	{
		if (_sysWmInfo.subsystem != SysWMType.Windows &&
			_sysWmInfo.subsystem != SysWMType.WinRT)
		{
			throw new PlatformNotSupportedException($"Cannot get HInstance for non-Windows subsystem '{_sysWmInfo.subsystem}'!");
		}

		try
		{
			// 'SDL_SysWMinfo.info' is a union of platform-specific data. Cast it straight to the windows-specific data layout:
			nint pInfoUnion = (nint)(&_sysWmInfo.info);

			WindowsWMInfo* pWindowsInfo = (WindowsWMInfo*)pInfoUnion;

			return pWindowsInfo->hInstance;
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to get HInstance from windows-specfic blob of {nameof(SDL_SysWMinfo)} struct!", ex);
		}
	}

	#endregion
}
