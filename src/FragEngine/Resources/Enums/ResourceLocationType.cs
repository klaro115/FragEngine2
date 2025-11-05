namespace FragEngine.Resources.Enums;

/// <summary>
/// Enumeration of different storage location where a resource's data may be loaded from.
/// </summary>
public enum ResourceLocationType
{
	/// <summary>
	/// The resource data is contained within a regular file that lies within the app's assets directory.
	/// The resource's data path is a file path relative to the resource manifest file's location.
	/// </summary>
	AssetFile		= 0,
	/// <summary>
	/// The resource data is in embedded file within either the app's main assembly, or the engine's assembly.
	/// The resource's data path is an embedded asssembly file path.
	/// </summary>
	EmbeddedFile,
	/// <summary>
	/// The resource data is loaded from a remote location accessible via network connection.
	/// The resource's data path is a web URL.
	/// </summary>
	Network,
	/// <summary>
	/// The resource data is procedurally generated. It has no storage location and is instead generated at run-time.
	/// The resource's data path is either unused, or provides a seed for generating the resource.
	/// </summary>
	Procedural,

	/// <summary>
	/// The ressource data is located at an unknown or unsupported location.
	/// </summary>
	Unknown,
}
