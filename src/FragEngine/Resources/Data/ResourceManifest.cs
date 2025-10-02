using FragEngine.EngineCore.Enums;
using Veldrid;

namespace FragEngine.Resources.Data;

/// <summary>
/// A serializable container and description object, defining one or more resources that may be loaded by the engine.
/// </summary>
[Serializable]
public sealed class ResourceManifest
{
	#region Properties

	// RESOURCES:

	/// <summary>
	/// An array of resource descriptions.
	/// </summary>
	public required ResourceData[] Resources { get; init; } = [];

	// REQUIREMENTS:

	/// <summary>
	/// Optional. If non-null, the manifest's resources may be used exclusively in this operating system environment.
	/// </summary>
	public OperatingSystemType? OSRestriction { get; init; } = null;
	/// <summary>
	/// Optional. If non-null, the manifest's resources may be used exclusively with this graphics backend/API.
	/// </summary>
	public GraphicsBackend? GraphicsRestriction { get; init; } = null;

	#endregion
}
