using FragEngine.Graphics.Enums;
using Veldrid;

namespace FragEngine.Extensions.Veldrid;

/// <summary>
/// Extension methods for the <see cref="GraphicsDeviceFeatures"/> class.
/// </summary>
public static class GraphicsDeviceFeaturesExt
{
	#region Methods

	/// <summary>
	/// Converts all boolean feature flags into bit flags in an enum value.
	/// </summary>
	/// <remarks>
	/// Note: Some tasks require one or multiple optional hardware features to be supported. When checking
	/// if those features are present at run-time, it is easier and faster to just apply a bit mask to the
	/// graphics device's feature flags enum, than to query features and then check a bunch of bool values.
	/// This method and the feature flags enum were added to facilitate such lookups, and to reduce the
	/// performance ovberhead of the check to a single clock cycle. Is this premature optimization? Perhaps,
	/// but it also doesn't hurt or make the consuming code more complicated.
	/// </remarks>
	/// <param name="_features">A structure describing the graphics device's supported features.</param>
	/// <returns>An enum with bit flages raised for each supported GPU feature.</returns>
	/// <exception cref="ArgumentNullException">Features may not be null.</exception>
	public static GraphicsDeviceFeatureFlags GetEnumFlags(this GraphicsDeviceFeatures _features)
	{
		ArgumentNullException.ThrowIfNull(_features);

		GraphicsDeviceFeatureFlags flags = 0;

		if (_features.ComputeShader)            flags |= GraphicsDeviceFeatureFlags.ComputeShader;
		if (_features.GeometryShader)           flags |= GraphicsDeviceFeatureFlags.GeometryShader;
		if (_features.TessellationShaders)      flags |= GraphicsDeviceFeatureFlags.TessellationShaders;
		if (_features.MultipleViewports)        flags |= GraphicsDeviceFeatureFlags.MultipleViewports;
		if (_features.SamplerLodBias)           flags |= GraphicsDeviceFeatureFlags.SamplerLodBias;
		if (_features.DrawBaseVertex)           flags |= GraphicsDeviceFeatureFlags.DrawBaseVertex;
		if (_features.DrawIndirect)             flags |= GraphicsDeviceFeatureFlags.DrawIndirect;
		if (_features.DrawIndirectBaseInstance) flags |= GraphicsDeviceFeatureFlags.DrawIndirectBaseInstance;
		if (_features.FillModeWireframe)        flags |= GraphicsDeviceFeatureFlags.FillModeWireframe;
		if (_features.SamplerAnisotropy)        flags |= GraphicsDeviceFeatureFlags.SamplerAnisotropy;
		if (_features.DepthClipDisable)         flags |= GraphicsDeviceFeatureFlags.DepthClipDisable;
		if (_features.Texture1D)                flags |= GraphicsDeviceFeatureFlags.Texture1D;
		if (_features.IndependentBlend)         flags |= GraphicsDeviceFeatureFlags.IndependentBlend;
		if (_features.StructuredBuffer)         flags |= GraphicsDeviceFeatureFlags.StructuredBuffer;
		if (_features.SubsetTextureView)        flags |= GraphicsDeviceFeatureFlags.SubsetTextureView;
		if (_features.CommandListDebugMarkers)  flags |= GraphicsDeviceFeatureFlags.CommandListDebugMarkers;
		if (_features.BufferRangeBinding)       flags |= GraphicsDeviceFeatureFlags.BufferRangeBinding;
		if (_features.ShaderFloat64)            flags |= GraphicsDeviceFeatureFlags.ShaderFloat64;

		return flags;
	}

	#endregion
}
