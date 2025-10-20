using FragEngine.EngineCore.Time;
using FragEngine.Interfaces;

namespace FragEngine.Graphics.Settings;

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
	/// Whether to synchronize the main window swap chain and its graphics device to the screen's refresh rate.
	/// If null, the V-Sync settings from graphics config are used instead.
	/// </summary>
	public bool? VSync { get; init; } = null;

	/// <summary>
	/// An upper limit for the frame rate at which the engine is allowed to render, in Hertz.
	/// To disable frame rate limiting, just set this to an absurdly high value.
	/// </summary>
	public float FrameRateLimit { get; set; } = 240;

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
		bool isFrameRateValid = FrameRateLimit > 0.01;

		return isFrameRateValid;
	}

	private ulong CalculateChecksum()
	{
		ulong newChecksum = 0ul;

		// 10-bit frame rate limit, with half-frame resolution:
		newChecksum |= (ulong)Math.Clamp(FrameRateLimit * 2, 0, 1024) << 0;

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
			VSync = _config.VSync,
			//...
		};
		return settings;
	}

	/// <summary>
	/// Creates a set of settings from the current state of the engine's services' state.
	/// </summary>
	/// <param name="_graphicsService">The engine's graphics service.</param>
	/// <param name="_timeService">The engine's time management service.</param>
	/// <returns>A new graphics settings object.</returns>
	/// <exception cref="ArgumentNullException">Graphics service may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Graphics service may not be disposed.</exception>
	public static GraphicsSettings CreateFromEngineServiceStates(in GraphicsService _graphicsService, in TimeService? _timeService)
	{
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ObjectDisposedException.ThrowIf(_graphicsService.IsDisposed, _graphicsService);

		bool vSync = _graphicsService.MainWindow is not null
			? _graphicsService.Device.SyncToVerticalBlank
			: _graphicsService.MainSwapchain is not null && _graphicsService.MainSwapchain.SyncToVerticalBlank;

		float targetFrameRate = _timeService is not null
			? _timeService.TargetFrameRate
			: 1000;

		GraphicsSettings settings = new()
		{
			VSync = vSync,
			FrameRateLimit = targetFrameRate,
			//...
		};
		return settings;
	}

	#endregion
}
