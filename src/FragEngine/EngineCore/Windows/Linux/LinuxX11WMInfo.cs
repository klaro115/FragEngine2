using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Veldrid.Sdl2;

namespace FragEngine.EngineCore.Windows.Linux;

/// <summary>
/// Structure corresponding to the X11-specfic blob in the <see cref="SDL_SysWMinfo.info"/> union.
/// This is platform-specfic window manager information about a specific SDL2 window on a Linux system
/// using the X11 window system.
/// </summary>
[SupportedOSPlatform("linux")]
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 64)]
public readonly record struct LinuxX11WMInfo
{
	/// <summary>
	/// X11 display handle.
	/// </summary>
	public readonly nint display;
	/// <summary>
	/// X11 window handle.
	/// </summary>
	public readonly nint window;
}
