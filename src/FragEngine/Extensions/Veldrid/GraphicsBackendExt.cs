using System.Runtime.CompilerServices;
using Veldrid;

namespace FragEngine.Extensions.Veldrid;

public static class GraphicsBackendExt
{
	#region Methods

	/// <summary>
	/// Gets whether this graphics backend is supported on the current executing OS.
	/// </summary>
	/// <remarks>
	/// This method wraps several compile-time evaluated platform switches.
	/// Intellisense and other code analyzers may however not understand that this qualifies as a platform guard.
	/// </remarks>
	/// <param name="_backend">This graphics backend.</param>
	/// <returns>True if backend is supported, false otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSupportedOnCurrentPlatform(this GraphicsBackend _backend)
	{
		return _backend switch
		{
			GraphicsBackend.Direct3D11 =>
				OperatingSystem.IsWindows(),

			GraphicsBackend.OpenGL or
			GraphicsBackend.OpenGLES or
			GraphicsBackend.Vulkan =>
				OperatingSystem.IsAndroid() ||
				OperatingSystem.IsFreeBSD() ||
				OperatingSystem.IsLinux() ||
				OperatingSystem.IsWindows(),

			GraphicsBackend.Metal =>
				OperatingSystem.IsIOS() ||
				OperatingSystem.IsMacOS() ||
				OperatingSystem.IsMacCatalyst(),

			_ => false,
		};
	}

	#endregion
}
