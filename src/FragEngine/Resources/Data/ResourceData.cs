using FragEngine.EngineCore.Enums;
using FragEngine.Interfaces;
using FragEngine.Resources.Enums;
using System.Text.Json.Serialization;
using Veldrid;

namespace FragEngine.Resources.Data;

/// <summary>
/// A serializable data object representing and identifying a resource/asset that may be loaded by the
/// engine's resource services.
/// </summary>
[Serializable]
public sealed record class ResourceData : IValidated
{
	#region Properties

	// IDENTIFIERS:

	/// <summary>
	/// A unique identifier of this resource.
	/// </summary>
	public required string ResourceKey { get; init; }
	/// <summary>
	/// The resource key of a fallback or backup resource, to use instead if this resource cannot be used.
	/// </summary>
	public string? FallbackResourceKey { get; init; } = null;

	// DATA LOCATION:

	/// <summary>
	/// The type of location where the resource's data is loaded from. Typically, whether the resource
	/// data is loaded from asset files or from an embedded assembly file.
	/// </summary>
	/// <remarks>
	/// Note: This property is not serialized to JSON. When scanning for resources, this value will be
	/// set to the actual location of the manifest file that contains this resource data definition.
	/// </remarks>
	[JsonIgnore]
	public ResourceLocationType Location { get; init; } = ResourceLocationType.AssetFile;
	/// <summary>
	/// A relative file path leading to the resource's data file. This path is relative to the resource
	/// manifest file that this resource data was read from.
	/// </summary>
	public required string RelativePath { get; init; }
	/// <summary>
	/// Starting position of the resource's data within its data file.
	/// </summary>
	///	<remarks>
	///	Warning: This offset value is agnostic to compressions or encryptions of the data file, and does
	///	not have a defined unit. If the data file is compressed or the data follows some alignment rules,
	///	the resource services will not know; it is left at the discretion of importer and file source to
	///	interprete this value correctly. The offset of most uncompressed resources should however be
	///	denoted in bytes.
	///	</remarks>
	public uint DataOffset { get; init; } = 0;
	/// <summary>
	/// Total size of the resource's data within its data file.
	/// </summary>
	/// ///	<remarks>
	///	Warning: This size value is agnostic to compressions or encryptions of the data file, and does
	///	not have a defined unit. If the data file is compressed or the data follows some alignment rules,
	///	the resource services will not know; it is left at the discretion of importer and file source to
	///	interprete this value correctly. The size of most uncompressed resources should however be
	///	denoted in bytes.
	///	</remarks>
	public required uint DataSize { get; init; }

	// DATA DESCRIPTION:

	/// <summary>
	/// A unique identifier for an importer or a file format. The engine's resource services will use
	/// this key to identify the correct file sources and importers for the resource.
	/// </summary>
	/// <remarks>
	/// If the format key is a file extension, it should be all lowercase and it must include the leading
	/// period character.<br/>
	/// Examples: <c>".jpg"</c>, <c>".fbx"</c>
	/// </remarks>
	public required string FormatKey { get; init; }
	/// <summary>
	/// The broad type of the resource.
	/// </summary>
	public required ResourceType Type { get; init; }
	/// <summary>
	/// Optional. The ID of a sub-type of the resource's type category.
	/// </summary>
	public int SubType { get; init; } = 0;

	// REQUIREMENTS:

	/// <summary>
	/// Optional. If non-null, the resource may be used exclusively in this operating system environment.
	/// </summary>
	public OperatingSystemType? OSRestriction { get; init; } = null;
	/// <summary>
	/// Optional. If non-null, the resource may be used exclusively with this graphics backend/API.
	/// </summary>
	public GraphicsBackend? GraphicsRestriction { get; init; } = null;

	#endregion
	#region Methods

	public bool IsValid()
	{
		bool isValid =
			!string.IsNullOrWhiteSpace(ResourceKey) &&
			!string.IsNullOrEmpty(FormatKey) &&
			Type != ResourceType.Unknown &&
			Location != ResourceLocationType.Unknown &&
			OSRestriction is not OperatingSystemType.Unknown;
		return isValid;
	}

	#endregion
}
