using Veldrid;

namespace FragEngine.Graphics.Shaders.Export;

/// <summary>
/// Interface for exporter types that can write a shader program to resources.
/// </summary>
public interface IShaderExporter
{
	/// <summary>
	/// Tries to write a shader program's binary data or source code to a resource.
	/// </summary>
	/// <param name="_resourceStream">A resource stream to which the shader data may be saved. This stream is
	/// assumed to be write-only. This will usually be a file stream.</param>
	/// <param name="_shader">The shader program that shall be written out as a resource.</param>
	///	<param name="_sourceCode">Original source code from which the shader was compiled.
	///	If null, only compiled binary data may be available, or the exporter might require shader reflection.</param>
	/// <returns>True if the shader asset was successfully written to stream, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource stream, shader program, or source code may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Shader program may not be disposed.</exception>
	/// <exception cref="InvalidOperationException">Writing shader binary data is not supported, or shader reflection is not available.</exception>
	bool WriteShaderProgram(Stream _resourceStream, in Shader _shader, string? _sourceCode = null);
}
