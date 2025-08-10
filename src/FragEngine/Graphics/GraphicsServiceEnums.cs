namespace FragEngine.Graphics;

/// <summary>
/// Flag with features that need to be initialized by the <see cref="GraphicsService"/>.
/// </summary>
[Flags]
public enum GraphicsServiceInitFlags
{
	/// <summary>
	/// Create the graphics device. This flag is mandatory.
	/// </summary>
	CreateDevice					= 1,
	/// <summary>
	/// Create a main window and swapchain for the graphics device.
	/// </summary>
	/// <remarks>
	/// You may discard this flag for headless clients that don't need output to a window.
	/// </remarks>
	CreateMainWindowAndSwapchain	= 2,
}
