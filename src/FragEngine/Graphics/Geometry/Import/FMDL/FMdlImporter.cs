using FragEngine.Extensions;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics.Geometry.Import.FMDL;

/// <summary>
/// 3D model importer for the engine's own FMDL (Fragment MoDeL) file format.
/// </summary>
public sealed class FMdlImporter(ILogger _logger) : IModelImporter
{
	#region Fields

	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

	#endregion
	#region Methods

	public bool LoadMeshSurfaceData(Stream _resourceStream, [NotNullWhen(true)] out MeshSurfaceData? _outSurfaceData)
	{
		ArgumentNullException.ThrowIfNull(_resourceStream);

		if (!_resourceStream.CanRead)
		{
			throw new ArgumentException("Cannot load FMDL surface data from write-only resource stream!", nameof(_resourceStream));
		}

		using BinaryReader reader = new(_resourceStream);

		try
		{
			return LoadMeshSurfaceData_Internal(reader, out _outSurfaceData);
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to load mesh surface data from FMDL file!", ex, LogEntrySeverity.Normal);
			_outSurfaceData = null;
			return false;
		}
	}

	private bool LoadMeshSurfaceData_Internal(BinaryReader _reader, [NotNullWhen(true)] out MeshSurfaceData? _outSurfaceData)
	{
		long fileStartPosition = _reader.BaseStream.Position;

		// File header:
		if (!FMdlFileHeader.Read(_reader, _logger, out FMdlFileHeader fileHeader))
		{
			logger.LogError("Failed to read FMDL file header!");
			_outSurfaceData = null;
			return false;
		}

		// Geometry header:
		long targetPosition = fileStartPosition + fileHeader.headerOffset;
		_reader.JumpTo(targetPosition);

		if (!FMdlGeometryHeader.Read(_reader, _logger, out FMdlGeometryHeader geometryHeader))
		{
			logger.LogError("Failed to read FMDL geometry header!");
			_outSurfaceData = null;
			return false;
		}

		//... (read additional header here)


		// Vertex data:
		long geometryStartPosition = fileStartPosition + fileHeader.dataOffset;
		targetPosition = geometryStartPosition;
		_reader.JumpTo(targetPosition);

		if (!ReadVertexData(_reader, in geometryHeader, out BasicVertex[] verticesBasic, out ExtendedVertex[]? verticesExt))
		{
			logger.LogError("Failed to read FMDL vertex geometry data!");
			_outSurfaceData = null;
			return false;
		}

		// Index data:
		long indexDataStartPosition = geometryStartPosition + geometryHeader.CalculateVertexDataSize();
		targetPosition = indexDataStartPosition;
		_reader.JumpTo(targetPosition);

		if (!ReadIndexData(_reader, in geometryHeader, out ushort[]? indices16, out int[]? indices32))
		{
			logger.LogError("Failed to read FMDL index geometry data!");
			_outSurfaceData = null;
			return false;
		}

		// Additional geometry data:
		targetPosition = indexDataStartPosition + geometryHeader.CalculateIndexDataSize();
		_reader.JumpTo(targetPosition);


		//... (read additional geometry data here)


		// Create and populate final surface data object:
		bool success = true;
		_outSurfaceData = new();

		success &= _outSurfaceData.SetVertices(verticesBasic, verticesExt, (int)geometryHeader.vertexCount, logger);

		if (indices16 is not null)
		{
			success &= _outSurfaceData.SetIndices16(indices16, (int)geometryHeader.indexCount, IndexFormat.UInt16, logger);
		}
		else if (indices32 is not null)
		{
			success &= _outSurfaceData.SetIndices32(indices32!, (int)geometryHeader.indexCount, IndexFormat.UInt32, logger);
		}
		else
		{
			logger.LogError("Failed to set index data of FMDL mesh; invalid index format!");
			return false;
		}

		// Advance resource stream to end of file:
		targetPosition = fileStartPosition + fileHeader.fileSize;
		_reader.JumpTo(targetPosition);

		return success;
	}

	private static unsafe bool ReadVertexData(
		BinaryReader _reader,
		in FMdlGeometryHeader _geometryHeader,
		out BasicVertex[] _outVerticesBasic,
		out ExtendedVertex[]? _outVerticesExt)
	{
		if (_geometryHeader.vertexCount == 0)
		{
			_outVerticesBasic = [];
			_outVerticesExt = null;
			return true;
		}

		// Basic vertex data:

		_outVerticesBasic = new BasicVertex[_geometryHeader.vertexCount];
		for (int i = 0; i < _geometryHeader.vertexCount; ++i)
		{
			_outVerticesBasic[i] = _reader.BaseStream.ReadStruct<BasicVertex>(BasicVertex.byteSize);
		}

		if (!_geometryHeader.hasExtendedVertexData)
		{
			_outVerticesExt = null;
			return true;
		}

		// Extended vertex data:

		_outVerticesExt = new ExtendedVertex[_geometryHeader.vertexCount];
		for (int i = 0; i < _geometryHeader.vertexCount; ++i)
		{
			_outVerticesExt[i] = _reader.BaseStream.ReadStruct<ExtendedVertex>(ExtendedVertex.byteSize);
		}

		return true;
	}

	private static bool ReadIndexData(
		BinaryReader _reader,
		in FMdlGeometryHeader _geometryHeader,
		out ushort[]? _outIndices16,
		out int[]? _outIndices32)
	{
		if (_geometryHeader.indexCount == 0)
		{
			_outIndices16 = [];
			_outIndices32 = null;
			return true;
		}

		// 16-bit indices:

		if (_geometryHeader.indexFormat == IndexFormat.UInt16)
		{
			_outIndices16 = new ushort[_geometryHeader.indexCount];
			_outIndices32 = null;

			for (int i = 0; i < _geometryHeader.indexCount; ++i)
			{
				_outIndices16[i] = _reader.ReadUInt16();
			}
			return true;
		}

		// 32-bit indices:

		_outIndices16 = null;
		_outIndices32 = new int[_geometryHeader.indexCount];

		for (int i = 0; i < _geometryHeader.indexCount; ++i)
		{
			_outIndices32[i] = _reader.ReadInt32();
		}
		return true;
	}

	#endregion
}
