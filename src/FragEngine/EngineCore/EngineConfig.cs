using FragEngine.Graphics;
using FragEngine.Application;

namespace FragEngine.EngineCore;

/// <summary>
/// Engine settings that are read once at launch time.
/// These are settings and configuration options that require a full app restart to take effect.
/// </summary>
[Serializable]
public sealed class EngineConfig
{
	#region Properties

	/// <summary>
	/// The graphics configuration.
	/// </summary>
	public required GraphicsConfig Graphics { get; init; } = new() { PreferNativeGraphicsAPI = true };

	/// <summary>
	/// Whether to create the main window and graphics device immediately on startup. Should be true in most cases.
	/// </summary>
	public required bool CreateMainWindowImmediately { get; init; } = true;

	/// <summary>
	/// Whether the engine's <see cref="IAppLogic"/> instance should be registered as a service that can be accessed using
	/// dependency injection. By default, this should be false, and app logic should operate in a strictly top-down manner.
	/// </summary>
	public bool AddAppLogicToServiceProvider { get; init; } = false;

	#endregion
	#region Methods

	/// <summary>
	/// Creates a very basic one-size-fits-all config. This will be used by default if the config can't be read from file.
	/// </summary>
	/// <returns>A default config.</returns>
	public static EngineConfig CreateDefault()
	{
		EngineConfig config = new()
		{
			Graphics = new()
			{
				PreferNativeGraphicsAPI = true
			},
			CreateMainWindowImmediately = true,
		};
		return config;
	}

	#endregion
}
