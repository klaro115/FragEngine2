using Veldrid;

namespace FragEngine.Graphics.Constants;

/// <summary>
/// Constants for the engine's graphics system.
/// </summary>
public static class GraphicsConstants
{
	#region Constants

	/// <summary>
	/// Gets a value representing an unknown or invalid graphics backend type.
	/// </summary>
	public const GraphicsBackend invalidBackend = (GraphicsBackend)255;

	/// <summary>
	/// Initial value for invalid or uninitialized checksums. If a checksum has this value, it needs to be
	/// recalculated, or no settings have been applied yet.
	/// </summary>
	internal const ulong UNINITIALIZED_CHECKSUM = 0ul;

	#endregion
}
