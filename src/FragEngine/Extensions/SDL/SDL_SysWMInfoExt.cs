using FragEngine.EngineCore.Windows.Linux;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Veldrid.Sdl2;

namespace FragEngine.Extensions.SDL;

/// <summary>
/// Extension methods for the <see cref="SDL_SysWMinfo"/> union struct.
/// </summary>
public static class SDL_SysWMInfoExt
{
	#region Constants

	public const int maximumWMInfoByteSize = 64;

	#endregion
	#region Types

	[SupportedOSPlatform("windows")]
	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = maximumWMInfoByteSize)]
	private readonly record struct WindowsWMInfo
	{
		public readonly nint hWnd;
		public readonly nint hDC;
		public readonly nint hInstance;
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
			// 'SDL_SysWMinfo.info' is a union of platform-specific data. Cast it straight to the Windows-specific data layout:
			nint pInfoUnion = (nint)(&_sysWmInfo.info);

			WindowsWMInfo* pWindowsInfo = (WindowsWMInfo*)pInfoUnion;

			return pWindowsInfo->hInstance;
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to get HInstance from windows-specfic blob of {nameof(SDL_SysWMinfo)} struct!", ex);
		}
	}

	/// <summary>
	/// Gets the Wayland info of an SDL window.
	/// </summary>
	/// <remarks>
	/// This method is available on Linux platforms only, and expects the Wayland window manager.
	/// </remarks>
	/// <param name="_sysWmInfo">Window manager info for an SDL window.</param>
	/// <returns>The window's Wayland window manager info, or all-null pointers, if the window info could not be retrieved.</returns>
	/// <exception cref="PlatformNotSupportedException">Method was called on a non-linux platform, or a Linux DE that doesn't use Wayland.</exception>
	/// <exception cref="Exception">Failed to get window manager info from wayland-specific blob in info union. This is likely a pointer or casting error.</exception>
	[SupportedOSPlatform("linux")]
	public static unsafe LinuxWaylandWMInfo GetLinuxWaylandInfo(this SDL_SysWMinfo _sysWmInfo)
	{
		if (_sysWmInfo.subsystem != SysWMType.Wayland)
		{
			throw new PlatformNotSupportedException($"Cannot get Wayland info for non-Wayland subsystem '{_sysWmInfo.subsystem}'!");
		}

		try
		{
			// 'SDL_SysWMinfo.info' is a union of platform-specific data. Cast it straight to the Wayland-specific data layout:
			nint pInfoUnion = (nint)(&_sysWmInfo.info);

			LinuxWaylandWMInfo waylandInfo = *(LinuxWaylandWMInfo*)pInfoUnion;

			return waylandInfo;
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to get Wayland info for Wayland-specific blob of {nameof(SDL_SysWMinfo)} struct!", ex);
		}
	}

	/// <summary>
	/// Gets the X11 info of an SDL window.
	/// </summary>
	/// <remarks>
	/// This method is available on Linux platforms only, and expects the X11 window system.
	/// </remarks>
	/// <param name="_sysWmInfo">Window manager info for an SDL window.</param>
	/// <returns>The window's X11 window manager info, or all-null pointers, if the window info could not be retrieved.</returns>
	/// <exception cref="PlatformNotSupportedException">Method was called on a non-linux platform, or a Linux DE that doesn't use X11.</exception>
	/// <exception cref="Exception">Failed to get window manager info from wayland-specific blob in info union. This is likely a pointer or casting error.</exception>
	[SupportedOSPlatform("linux")]
	public static unsafe LinuxX11WMInfo GetLinuxX11Info(this SDL_SysWMinfo _sysWmInfo)
	{
		if (_sysWmInfo.subsystem != SysWMType.X11)
		{
			throw new PlatformNotSupportedException($"Cannot get X11 info for non-X11 subsystem '{_sysWmInfo.subsystem}'!");
		}

		try
		{
			// 'SDL_SysWMinfo.info' is a union of platform-specific data. Cast it straight to the X11-specific data layout:
			nint pInfoUnion = (nint)(&_sysWmInfo.info);

			LinuxX11WMInfo waylandInfo = *(LinuxX11WMInfo*)pInfoUnion;

			return waylandInfo;
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to get X11 info for X11-specific blob of {nameof(SDL_SysWMinfo)} struct!", ex);
		}
	}

	#endregion
}
