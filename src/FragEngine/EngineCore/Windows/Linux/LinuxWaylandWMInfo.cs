using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Veldrid.Sdl2;

namespace FragEngine.EngineCore.Windows.Linux;

/// <summary>
/// Structure corresponding to the Wayland-specfic blob in the <see cref="SDL_SysWMinfo.info"/> union.
/// This is platform-specfic window manager information about a specific SDL2 window on a Linux system
/// using the Wayland window manager.
/// </summary>
[SupportedOSPlatform("linux")]
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 64)]
public readonly record struct LinuxWaylandWMInfo
{
	/// <summary>
	/// Wayland display proxy.
	/// </summary>
	public readonly nint display;
	/// <summary>
	/// Wayland surface proxy.
	/// </summary>
	public readonly nint surface;
	/// <summary>
	/// Wayland shell surface proxy.
	/// </summary>
	public readonly nint shellSurface;
}
