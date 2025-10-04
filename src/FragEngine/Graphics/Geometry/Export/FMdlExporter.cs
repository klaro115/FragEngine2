using FragEngine.Extensions;
using FragEngine.Graphics.Geometry.Import.FMDL;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.Graphics.Geometry.Export;

/// <summary>
/// 3D model exporter for the engine's own FMDL (Fragment MoDeL) file format.
/// </summary>
public sealed class FMdlExporter(ILogger _logger) : IModelExporter
{
	#region Fields

	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

	#endregion
	#region Methods

	public bool WriteMeshSurfaceData(Stream _resourceStream, in MeshSurfaceData _surfaceData)
	{
		ArgumentNullException.ThrowIfNull(_resourceStream);

		if (!_resourceStream.CanWrite)
		{
			throw new ArgumentException("Cannot write FMDL surface data to read-only resource stream!", nameof(_resourceStream));
		}

		if (!_surfaceData.IsValid())
		{
			logger.LogError("Cannot write invalid or incomplete mesh surface data to FMDL format!");
			return false;
		}

		using BinaryWriter writer = new(_resourceStream);

		try
		{
			return WriteMeshSurfaceData_Internal(writer, _surfaceData);
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to write mesh surface data for FMDL file!", ex, LogEntrySeverity.Normal);
			return false;
		}
	}

	private bool WriteMeshSurfaceData_Internal(BinaryWriter _writer, in MeshSurfaceData _surfaceData)
	{
		long fileStartPosition = _writer.BaseStream.Position;

		// Construct all headers:
		FMdlGeometryHeader geometryHeader = new()
		{
			vertexCount = (uint)_surfaceData.VertexCount,
			indexCount = (uint)_surfaceData.IndexCount,
			indexFormat = _surfaceData.IndexFormat,

			hasExtendedVertexData = _surfaceData.HasExtendedVertexData,
			//...
		};

		ushort totalHeaderSize = FMdlFileHeader.byteSize + FMdlGeometryHeader.byteSize;
		uint totalDataSize = geometryHeader.CalculateTotalDataSize();
		uint totalFileSize = totalHeaderSize + totalDataSize;

		FMdlFileHeader fileHeader = new()
		{
			// General:
			magicNumbers = FMdlConstants.magicNumbers,
			fileSize = totalFileSize,
			versionMajor = FMdlConstants.currentVersionMajor,
			versionMinor = FMdlConstants.currentVersionMinor,

			// Headers:
			headerFlags = FMdlConstants.mandatoryHeaderFlags,
			headerOffset = 0,
			totalHeaderSize = totalHeaderSize,

			// Data:
			dataOffset = totalHeaderSize,
			totalDataSize = totalDataSize,
		};

		long targetPosition = fileStartPosition + fileHeader.headerOffset;
		_writer.JumpTo(targetPosition, true);

		// Write headers:
		if (!fileHeader.Write(_writer, _logger))
		{
			return false;
		}
		
		if (!geometryHeader.Write(_writer, _logger))
		{
			return false;
		}

		//... (add additional headers here)


		// Write geometry data:
		targetPosition = fileStartPosition + fileHeader.dataOffset;
		_writer.JumpTo(targetPosition, true);

		if (!WriteVertexData(_writer, in geometryHeader, in _surfaceData))
		{
			logger.LogError("Failed to write vertex data of FMDL file!");
			return false;
		}

		if (!WriteIndexData(_writer, in geometryHeader, in _surfaceData))
		{
			logger.LogError("Failed to write index data of FMDL file!");
			return false;
		}

		//... (add additional data here)


		// Advance writer to predicted end of file, then exit:
		targetPosition = fileStartPosition + fileHeader.fileSize;
		if (targetPosition < _writer.BaseStream.Position)
		{
			logger.LogError("FMDL export wrote past predicted file size; sizes and offsets in headers may be incorrect!");
			return false;
		}
		_writer.JumpTo(targetPosition, true);

		return true;
	}

	private static bool WriteVertexData(BinaryWriter _writer, in FMdlGeometryHeader _geometryHeader, in MeshSurfaceData _surfaceData)
	{
		if (_geometryHeader.vertexCount == 0)
		{
			return true;
		}

		// Basic vertex data:
		for (int i = 0; i < _geometryHeader.vertexCount; ++i)
		{
			_writer.BaseStream.WriteStruct(_surfaceData.VerticesBasic[i], BasicVertex.byteSize);
		}

		if (!_geometryHeader.hasExtendedVertexData)
		{
			return true;
		}

		// Extended vertex data:
		for (int i = 0; i < _geometryHeader.vertexCount; ++i)
		{
			_writer.BaseStream.WriteStruct(_surfaceData.VerticesExt![i], ExtendedVertex.byteSize);
		}

		return true;
	}

	private static bool WriteIndexData(BinaryWriter _writer, in FMdlGeometryHeader _geometryHeader, in MeshSurfaceData _surfaceData)
	{
		if (_geometryHeader.indexCount == 0)
		{
			return true;
		}

		// 16-bit index data:
		if (_geometryHeader.indexFormat == IndexFormat.UInt16)
		{
			for (int i = 0; i < _geometryHeader.indexCount; ++i)
			{
				_writer.Write(_surfaceData.Indices16![i]);
			}

			return true;
		}

		// 32-bit index data:
		for (int i = 0; i < _geometryHeader.indexCount; ++i)
		{
			_writer.Write(_surfaceData.Indices32![i]);
		}

		return true;
	}

	#endregion
}
