using FragEngine.Interfaces;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// Graphical settings that may be changed after launch.
/// </summary>
[Serializable]
public sealed class GraphicsSettings : IValidated
{
	#region Properties

	/// <summary>
	/// Index of the preferred output screen. If negative or invalid, the config's value is used.
	/// </summary>
	public int OutputScreenIndex { get; set; } = -1;

	/// <summary>
	/// Resolution to render the final output image at. This is the actual output resolution on screen,
	/// after any upscaling has taken place. If null, native screen size or FullHD will be used.
	/// </summary>
	public Vector2? OutputResolution { get; set; } = null;

	/// <summary>
	/// An upper limit for the frame rate at which the engine is allowed to render, in Hertz.
	/// To disable frame rate limiting, just set this to an absurdly high value.
	/// </summary>
	public float FrameRateLimit { get; set; } = 240;

	public WindowState WindowState { get; init; } = WindowState.Normal;

	#endregion
	#region Methods

	public bool IsValid()
	{
		bool isResolutionValid = OutputResolution is null || (OutputResolution.Value.X > 8 && OutputResolution.Value.Y > 0);
		bool isFrameRateValid = FrameRateLimit > 0.01;

		return isResolutionValid && isFrameRateValid;
	}

	public static GraphicsSettings CreateDefaultForConfig(GraphicsConfig _config)
	{
		GraphicsSettings settings = new()
		{
			OutputResolution = _config.FallbackOutputResolution,
			OutputScreenIndex = (int)_config.MainWindowScreenIndex,
		};
		return settings;
	}

	#endregion
}
