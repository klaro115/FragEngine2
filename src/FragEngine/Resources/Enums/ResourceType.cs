using Veldrid;

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
	Shader,

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
	VoxelGrid,
}

/// <summary>
/// Enumeration of resource sub-types for 3D models and geometry.
/// </summary>
public enum ResourceSubType_Shader : int
{
	Compute = 0,
	Vertex,
	Geometry,
	TesselationCtrl,
	TesselationEval,
	Pixel,
	//...
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

/************************************************************************************/
// EXTENSIONS:

/// <summary>
/// Extension methods for the <see cref="ResourceType"/> enum.
/// </summary>
public static class ResourceTypeExt
{
	#region Methods

	/// <summary>
	/// Tries to get the shader stage corresponding to this resource type and sub-type.
	/// </summary>
	/// <param name="_resourceType">This resource type. Should be <see cref="ResourceType.Shader"/>.</param>
	/// <param name="_resourceSubType">The sub-type of the shader resource, should map to a value of <see cref="ResourceSubType_Shader"/>.</param>
	/// <returns>A shader stage, or '<see cref="ShaderStages.None"/>' for non-shader resources and invalid sub-types.</returns>
	public static ShaderStages GetShaderStageForType(this ResourceType _resourceType, int _resourceSubType)
	{
		if (_resourceType != ResourceType.Shader)
		{
			return ShaderStages.None;
		}

		return (ResourceSubType_Shader)_resourceSubType switch
		{
			ResourceSubType_Shader.Compute => ShaderStages.Compute,
			ResourceSubType_Shader.Vertex => ShaderStages.Vertex,
			ResourceSubType_Shader.Geometry => ShaderStages.Geometry,
			ResourceSubType_Shader.TesselationCtrl => ShaderStages.TessellationControl,
			ResourceSubType_Shader.TesselationEval => ShaderStages.TessellationEvaluation,
			ResourceSubType_Shader.Pixel => ShaderStages.Fragment,
			_ => ShaderStages.None,
		};
	}

	#endregion
}
