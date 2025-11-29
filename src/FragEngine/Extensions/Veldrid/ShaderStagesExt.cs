using Veldrid;

namespace FragEngine.Extensions.Veldrid;

/// <summary>
/// Extension methods for the <see cref="ShaderStages"/> enum.
/// </summary>
public static class ShaderStagesExt
{
	#region Methods

	/// <summary>
	/// Gets a 2-letter name prefix corresponding to this shader stage type.<para/>
	/// <example>
	/// <b>Examples:</b><br/>
	/// <list type="bullet">
	///		<item><see cref="ShaderStages.Vertex"/> => <c>"VS"</c></item>
	///		<item><see cref="ShaderStages.Fragment"/> => <c>"PS"</c></item>
	///		<item><see cref="ShaderStages.Compute"/> => <c>"CS"</c></item>
	/// </list>
	/// </example>
	/// </summary>
	/// <param name="_stage">This shader stage.</param>
	/// <returns>A 2-letter string identifying the shader stage.</returns>
	public static string GetShaderNamePrefix(this ShaderStages _stage)
	{
		return _stage switch
		{
			ShaderStages.Vertex => "VS",
			ShaderStages.Geometry => "GS",
			ShaderStages.TessellationControl or ShaderStages.TessellationEvaluation => "TS",
			ShaderStages.Fragment => "PS",
			ShaderStages.Compute => "CS",
			_ => "??",
		};
	}

	/// <summary>
	/// Gets the resource key for the fallback shader corresponding to this resource stage.
	/// </summary>
	/// <remarks>
	/// <b>Format:</b> Resource keys for fallback shaders are always in the format <c>"XS_Fallback"</c>, where <c>XS</c>
	/// is replaced by the name prefix of its shader stage. Use '<see cref="GetShaderNamePrefix(ShaderStages)"/>' to get
	/// the appropriate prefix for each shader stage.<para/>
	/// <b>Note:</b> Fallback shaders are bundled with the engine assembly as embedded resource files. They only provide
	/// very basic functionality, but they might prevent your app from crashing out if used as a replacement for missing
	/// shader assets.
	/// </remarks>
	/// <param name="_stage">This shader stage.</param>
	/// <returns>A resource key identifying the fallback shader for this stage, or empty, if the stage is invalid.</returns>
	public static string GetFallbackShaderResourceKey(this ShaderStages _stage)
	{
		return _stage != ShaderStages.None
			? $"{GetShaderNamePrefix(_stage)}_Fallback"
			: string.Empty;
	}

	/// <summary>
	/// Gets the default entry point function name corresponding to this resource stage.
	/// </summary>
	/// <param name="_stage">This shader stage.</param>
	/// <returns>The name of the entry point function.</returns>
	public static string GetDefaultEntryPoint(this ShaderStages _stage)
	{
		return _stage switch
		{
			ShaderStages.Vertex => "MainVertex",
			ShaderStages.Geometry => "MainGeometry",
			ShaderStages.TessellationControl => "MainTesselationCtrl",
			ShaderStages.TessellationEvaluation => "MainTesselationEval",
			ShaderStages.Fragment => "MainPixel",
			ShaderStages.Compute => "MainCompute",
			_ => "Main",
		};
	}

	#endregion
}
