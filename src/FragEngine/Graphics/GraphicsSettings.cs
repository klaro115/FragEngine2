using FragEngine.Interfaces;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// Graphical settings that may be changed after launch.
/// </summary>
[Serializable]
public sealed class GraphicsSettings : IValidated, IChecksumVersioned
{
	#region Fields

	private ulong checksum = 0ul;

	#endregion
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
	/// Whether to synchronize the main window swap chain and its graphics device to the screen's refresh rate.
	/// If null, the V-Sync settings from graphics config are used instead.
	/// </summary>
	public bool? VSync { get; init; } = null;

	/// <summary>
	/// An upper limit for the frame rate at which the engine is allowed to render, in Hertz.
	/// To disable frame rate limiting, just set this to an absurdly high value.
	/// </summary>
	public float FrameRateLimit { get; set; } = 240;

	/// <summary>
	/// The desired window state to display the app in.
	/// Basically, whether to use windowed mode, fullscreen, or borderless window mode.
	/// </summary>
	public WindowState WindowState { get; init; } = WindowState.Normal;

	public ulong Checksum
	{
		get
		{
			if (checksum != 0)
			{
				return checksum;
			}

			checksum = CalculateChecksum();
			return checksum;
		}
	}

	#endregion
	#region Methods

	public bool IsValid()
	{
		bool isResolutionValid = OutputResolution is null || (OutputResolution.Value.X > 8 && OutputResolution.Value.Y > 0);
		bool isFrameRateValid = FrameRateLimit > 0.01;

		return isResolutionValid && isFrameRateValid;
	}

	private ulong CalculateChecksum()
	{
		ulong newChecksum = 0ul;

		// 4-bit screen index, 0b1111 meaning -1, for a maximum of 15 screens:
		ulong screenIdx = OutputScreenIndex >= 0
			? (ulong)Math.Min(OutputScreenIndex, 0b1110)
			: 0b1111;

		newChecksum |= screenIdx;

		// 13-bit each for X/Y screen resolution, for resolution between 8p - 8192p:
		if (OutputResolution is not null)
		{
			newChecksum |= (ulong)Math.Clamp(OutputResolution.Value.X, 8, 8192) << 4;
			newChecksum |= (ulong)Math.Clamp(OutputResolution.Value.Y, 8, 8192) << 17;
		}

		// 10-bit frame rate limit, with half-frame resolution:
		newChecksum |= (ulong)Math.Clamp(FrameRateLimit * 2, 0, 1024) << 30;

		// 3-bit window state:
		newChecksum |= (ulong)WindowState << 40;

		return newChecksum;
	}

	/// <summary>
	/// Creates a set of default settings that align with a given graphics configuration.
	/// </summary>
	/// <param name="_config">The graphics configuration.</param>
	/// <returns>A new graphics settings object.</returns>
	/// <exception cref="ArgumentNullException">Graphics config may not be null.</exception>
	public static GraphicsSettings CreateDefaultForConfig(GraphicsConfig _config)
	{
		ArgumentNullException.ThrowIfNull(_config);

		GraphicsSettings settings = new()
		{
			OutputResolution = _config.FallbackOutputResolution,
			OutputScreenIndex = (int)_config.MainWindowScreenIndex,
			VSync = _config.VSync,
		};
		return settings;
	}

	#endregion
}
