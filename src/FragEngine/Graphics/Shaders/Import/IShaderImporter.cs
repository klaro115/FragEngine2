using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics.Shaders.Import;

/// <summary>
/// Interface for importer types that can load a shader program from resources.
/// </summary>
public interface IShaderImporter
{
	#region Methods

	/// <summary>
	/// Tries to load a shader program from a resource.
	/// </summary>
	/// <param name="_resourceStream">A resource stream from which the shader data may be loaded. This stream is
	/// assumed to be read-only.</param>
	/// <param name="_shaderStage">The shader stage for which the source code should be compiled.</param>
	/// <param name="_entryPoint">Optional. The name of the entry point function for the shader program.
	/// If null, the default entry point function name for the given stage is used instead.</param>
	/// <param name="_outShader">Outputs the fully loaded shader program, or null, if the import failed.</param>
	/// <returns>True if the shader was loaded successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource stream may not be null.</exception>
	/// <exception cref="ArgumentException">Resource stream did not support reading, or shader stage is not supported.</exception>
	bool LoadShaderProgram(Stream _resourceStream, int _dataByteSize, ShaderStages _shaderStage, string? _entryPoint, [NotNullWhen(true)] out Shader? _outShader);

	#endregion
}
