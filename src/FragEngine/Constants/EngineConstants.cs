namespace FragEngine.Constants;

/// <summary>
/// Constants for general engine operation.
/// </summary>
public static class EngineConstants
{
	#region Constants

	// VERSIONS:

	private const int engineVersionMajor = 0;
	private const int engineVersionMinor = 0;
	private const int engineVersionPatch = 1;

	// NAMES:

	/// <summary>
	/// The engine's official name as it should be displayed publicly.
	/// </summary>
	public const string engineDisplayName = "Fragment Engine";

	/// <summary>
	/// The engine's official name, followed by the current version.
	/// </summary>
	public static readonly string engineVersionedName = $"{engineDisplayName} v{engineVersionMajor}.{engineVersionMinor}.{engineVersionPatch}";

	#endregion
	#region Properties

	/// <summary>
	/// Gets the engine's current version.
	/// </summary>
	/// <remarks>
	/// Reminder for engine devs: remember to keep these numbers up-to-date with new releases.
	/// </remarks>
	public static Version EngineVersion => new(engineVersionMajor, engineVersionMinor, engineVersionPatch);

	#endregion
}
