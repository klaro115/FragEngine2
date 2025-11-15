using Veldrid;

namespace FragEngine.Graphics.Enums;

/// <summary>
/// Enum bit flags representation of the boolean feature flags in the <see cref="GraphicsDeviceFeatures"/> struct.
/// Values of this type represent a number of features that are either available to a <see cref="GraphicsDevice"/>,
/// or that are required for a graphics operation.
/// </summary>
[Flags]
public enum GraphicsDeviceFeatureFlags
{
	/// <summary>
	/// Zero flag, i.e. no feature flags are raised.
	/// </summary>
	None						= 0,

	/// <inheritdoc cref="GraphicsDeviceFeatures.ComputeShader"/>
	ComputeShader				= 1 << 0,
	/// <inheritdoc cref="GraphicsDeviceFeatures.GeometryShader"/>
	GeometryShader				= 1 << 1,
	/// <inheritdoc cref="GraphicsDeviceFeatures.TessellationShaders"/>
	TessellationShaders			= 1 << 2,
	/// <inheritdoc cref="GraphicsDeviceFeatures.MultipleViewports"/>
	MultipleViewports			= 1 << 3,
	/// <inheritdoc cref="GraphicsDeviceFeatures.SamplerLodBias"/>
	SamplerLodBias				= 1 << 4,
	/// <inheritdoc cref="GraphicsDeviceFeatures.DrawBaseVertex"/>
	DrawBaseVertex				= 1 << 5,
	/// <inheritdoc cref="GraphicsDeviceFeatures.DrawIndirect"/>
	DrawIndirect				= 1 << 6,
	/// <inheritdoc cref="GraphicsDeviceFeatures.DrawIndirectBaseInstance"/>
	DrawIndirectBaseInstance	= 1 << 7,
	/// <inheritdoc cref="GraphicsDeviceFeatures.FillModeWireframe"/>
	FillModeWireframe			= 1 << 8,
	/// <inheritdoc cref="GraphicsDeviceFeatures.SamplerAnisotropy"/>
	SamplerAnisotropy			= 1 << 9,
	/// <inheritdoc cref="GraphicsDeviceFeatures.DepthClipDisable"/>
	DepthClipDisable			= 1 << 10,
	/// <inheritdoc cref="GraphicsDeviceFeatures.Texture1D"/>
	Texture1D					= 1 << 11,
	/// <inheritdoc cref="GraphicsDeviceFeatures.IndependentBlend"/>
	IndependentBlend			= 1 << 12,
	/// <inheritdoc cref="GraphicsDeviceFeatures.StructuredBuffer"/>
	StructuredBuffer			= 1 << 13,
	/// <inheritdoc cref="GraphicsDeviceFeatures.SubsetTextureView"/>
	SubsetTextureView			= 1 << 14,
	/// <inheritdoc cref="GraphicsDeviceFeatures.CommandListDebugMarkers"/>
	CommandListDebugMarkers		= 1 << 15,
	/// <inheritdoc cref="GraphicsDeviceFeatures.BufferRangeBinding"/>
	BufferRangeBinding			= 1 << 16,
	/// <inheritdoc cref="GraphicsDeviceFeatures.ShaderFloat64"/>
	ShaderFloat64				= 1 << 17,
}
