using FragEngine.Application;

namespace FragEngine.EngineCore.Config;

/// <summary>
/// Settings for the engine's initial setup and startup behaviour.
/// </summary>
[Serializable]
public sealed class EngineStartupConfig
{
	#region Properties

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
}
