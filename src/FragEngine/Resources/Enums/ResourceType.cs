namespace FragEngine.Resources.Enums;

/// <summary>
/// Enumeration of different broad categories of assets and resources.
/// </summary>
public enum ResourceType
{
	// MISC:

	Unknown			= 0,
	Custom,

	// GAME ASSETS:

	Texture			= 10,
	Video,
	Audio,
	Model,

	// PURE DATA:

	Buffer			= 30,
	SerializedData,
	Markup,
	Text,
	Database,

	// CODE & LOGIC:

	Assembly		= 60,
	Script,
	Program,
	//...
}

/************************************************************************************/
// SUB-TYPES:

/// <summary>
/// Enumeration of resource sub-types for textures.
/// </summary>
public enum ResourceSubType_Texture : int
{
	Texture1D		= 0,
	Texture2D,
	Texture3D,
	Cubemap,
}

/// <summary>
/// Enumeration of resource sub-types for audio.
/// </summary>
public enum ResourceSubType_Audio : int
{
	SoundEffect		= 0,
	Music,
	Voice,
	Sample,
}

/// <summary>
/// Enumeration of resource sub-types for 3D models and geometry.
/// </summary>
public enum ResourceSubType_Model : int
{
	PolygonMesh		= 0,
	SplineSurface,
}

/// <summary>
/// Enumeration of resource sub-types for serialized data.
/// Many of these are plaintext formats.
/// </summary>
public enum ResourceSubType_SerializedData : int
{
	JSON			= 0,
	XML,
	CSV,
	YML,
	//...
}

/// <summary>
/// Enumeration of resource sub-types for markup files.
/// Many of these are plaintext formats.
/// </summary>
public enum ResourceSubType_Markup : int
{
	Markdown		= 0,
	HTML,
	XAML,
	//...
}
