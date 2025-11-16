namespace FragEngine.Graphics.Cameras;

public static class CameraConstants
{
	#region Constants General

	/// <summary>
	/// Initial value for invalid or uninitialized checksums. If a checksum has this value, it needs to be
	/// recalculated, or no settings have been applied yet.
	/// </summary>
	internal const ulong UNINITIALIZED_CHECKSUM = 0ul;

	#endregion
	#region Constants Projection

	// PROJECTION:

	public const float defaultFieldOfViewDegrees = 60.0f;
	public const float defaultNearClipPlane = 0.1f;
	public const float defaultFarClipPlane = 1000.0f;
	public const float defaultOrthographicSize = 5.0f;

	#endregion
	#region Constants Output

	// OUTPUT:

	public const uint defaultOutputResolutionX = 640;
	public const uint defaultOutputResolutionY = 480;
	public const uint minOutputResolution = 8;
	public const uint maxOutputResolution = 8192;

	#endregion
}
