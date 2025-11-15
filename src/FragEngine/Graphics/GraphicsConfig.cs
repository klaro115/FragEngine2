using FragEngine.Graphics.Enums;
using FragEngine.Interfaces;
using System.Numerics;

namespace FragEngine.Graphics;

/// <summary>
/// Graphics settings that are read once at launch time.
/// </summary>
[Serializable]
public sealed class GraphicsConfig : IValidated
{
	#region Properties

	/// <summary>
	/// Whether to prefer the OS' native graphics API over a cross-platform API.
	/// </summary>
	/// <remarks>
	/// On Windows, the native API is Direct3D, and the cross-platform alternative is Vulkan.
	/// </remarks>
	public required bool PreferNativeGraphicsAPI { get; init; } = true;

	/// <summary>
	/// Index of the screen on which to create the main window. When in doubt, leave this 0.
	/// </summary>
	public uint MainWindowScreenIndex { get; init; } = 0u;

	/// <summary>
	/// Title of the main window; in general, this should match your application's name. If null, a default title is used.
	/// </summary>
	public string? MainWindowTitle { get; init; } = "Fragment Engine";

	/// <summary>
	/// A fallback value for the main render resolution and the output window size.
	/// </summary>
	public Vector2 FallbackOutputResolution { get; init; } = new(640, 480);

	/// <summary>
	/// Whether to center the main window on the screen.
	/// </summary>
	public bool CenterMainWindow { get; init; } = true;

	/// <summary>
	/// Whether to create the graphics device in debug mode. This may negatively impact performance.
	/// </summary>
	public bool CreateDebug { get; init; } = false;

	/// <summary>
	/// Whether to initialize the main window swap chain with V-Sync enabled.
	/// </summary>
	public bool VSync { get; init; } = true;

	/// <summary>
	/// Whether the main swapchain output should prefer sRGB formats.
	/// </summary>
	public bool OutputIsSRgb { get; init; } = true;

	/// <summary>
	/// Gets bit flags for all graphics device features that require support for the application to run correctly.
	/// If one or more of these features are unsupported by the GPU, the application will exit immediately.
	/// </summary>
	public GraphicsDeviceFeatureFlags MinimumDeviceFeatureRequirements { get; init; } = GraphicsDeviceFeatureFlags.None;

	#endregion
	#region Methods

	public bool IsValid()
	{
		if (FallbackOutputResolution.X < 8 || FallbackOutputResolution.Y < 8)
		{
			return false;
		}
		return true;
	}

	#endregion
}
