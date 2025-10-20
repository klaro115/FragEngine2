using FragEngine.Interfaces;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Settings;

/// <summary>
/// Display settings that may be changed after launch.
/// </summary>
[Serializable]
public sealed class DisplaySettings : IValidated, IChecksumVersioned
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
		bool isResolutionValid = OutputResolution is null || (OutputResolution.Value.X >= 8 && OutputResolution.Value.Y >= 0);
		return isResolutionValid;
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

		// 3-bit window state:
		newChecksum |= (ulong)WindowState << 30;

		return newChecksum;
	}

	/// <summary>
	/// Creates a set of default settings that align with a given graphics configuration.
	/// </summary>
	/// <param name="_config">The graphics configuration.</param>
	/// <returns>A new display settings object.</returns>
	/// <exception cref="ArgumentNullException">Graphics config may not be null.</exception>
	public static DisplaySettings CreateDefaultForConfig(GraphicsConfig _config)
	{
		ArgumentNullException.ThrowIfNull(_config);

		DisplaySettings settings = new()
		{
			OutputResolution = _config.FallbackOutputResolution,
			OutputScreenIndex = (int)_config.MainWindowScreenIndex,
			//...
		};
		return settings;
	}

	#endregion
}
