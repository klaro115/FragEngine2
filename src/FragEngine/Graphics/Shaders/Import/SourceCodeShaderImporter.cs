using FragEngine.EngineCore.Config;
using FragEngine.Extensions;
using FragEngine.Extensions.Veldrid;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics.Shaders.Import;

/// <summary>
/// Shader importer that compiles shaders from source code.
/// </summary>
/// <param name="_graphicsService">The engine's graphics service.</param>
/// <param name="_config">The engine's startup configuration settings.</param>
/// <param name="_logger">The engine's logging service.</param>
public sealed class SourceCodeShaderImporter(GraphicsService _graphicsService, EngineConfig _config, ILogger _logger) : IShaderImporter
{
	#region Fields

	private readonly GraphicsService graphicsService = _graphicsService ?? throw new ArgumentNullException(nameof(_graphicsService));
	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
	private readonly GraphicsConfig config = _config?.Graphics ?? throw new ArgumentNullException(nameof(_config));

	/// <summary>
	/// All supported file format extensions (aka format keys) that are supported by this importer.
	/// </summary>
	public static readonly string[] supportedFormatKeys =
	[
		".hlsl",
		".glsl",
		".metal",
		".spirv",
	];

	#endregion
	#region Methods

	public bool LoadShaderProgram(Stream _resourceStream, int _dataByteSize, ShaderStages _shaderStage, string? _entryPoint, [NotNullWhen(true)] out Shader? _outShader)
	{
		ArgumentNullException.ThrowIfNull(_resourceStream);

		if (!_resourceStream.CanRead)
		{
			throw new ArgumentException("Cannot load shader from write-only resource stream!", nameof(_resourceStream));
		}
		if (_shaderStage == ShaderStages.None)
		{
			throw new ArgumentException("Cannot load shader for invalid shader stage!", nameof(_shaderStage));
		}

		using BinaryReader reader = new(_resourceStream);

		try
		{
			return LoadShaderProgram_Internal(reader, _dataByteSize, _shaderStage, _entryPoint, out _outShader);
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to load mesh surface data from FMDL file!", ex, LogEntrySeverity.Normal);
			_outShader = null;
			return false;
		}
	}

	private bool LoadShaderProgram_Internal(BinaryReader _reader, int _dataByteSize, ShaderStages _shaderStage, string? _entryPoint, [NotNullWhen(true)] out Shader? _outShader)
	{
		long fileStartPosition = _reader.BaseStream.Position;
		long fileEndPosition = fileStartPosition + _dataByteSize;

		if (string.IsNullOrWhiteSpace(_entryPoint))
		{
			_entryPoint = _shaderStage.GetDefaultEntryPoint();
		}

		// Read source code byte data from stream:
		byte[] bytes = new byte[_dataByteSize];
		int actualByteSize;
		try
		{
			actualByteSize = _reader.Read(bytes);
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to read shader source code from stream!", ex);
			_outShader = null;
			return false;
		}

		// Trim byte data if mismatched, and log a warning:
		if (actualByteSize < _dataByteSize)
		{
			byte[] trimmedBytes = new byte[actualByteSize];
			Array.Copy(bytes, trimmedBytes, actualByteSize);
			bytes = trimmedBytes;

			logger.LogWarning($"Data size mismatch for shader source code! (Expected: {_dataByteSize}, Actual: {actualByteSize})");
		}

		// Compile shader and upload it to GPU:
		ShaderDescription desc = new(_shaderStage, bytes, _entryPoint, config.CreateDebug);
		try
		{
			_outShader = graphicsService.ResourceFactory.CreateShader(ref desc);
			_outShader.Name = $"{_shaderStage.GetShaderNamePrefix()}_{_entryPoint}";
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create fallback shader for stage '{_shaderStage}'!", ex);
			_outShader = null;
			return false;
		}

		// Advance reader to EOF:
		_reader.JumpTo(fileEndPosition);
		return true;
	}

	#endregion
}
