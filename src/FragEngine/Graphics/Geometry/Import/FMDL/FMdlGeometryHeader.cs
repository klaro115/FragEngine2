using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Interfaces;
using Veldrid;

namespace FragEngine.Graphics.Geometry.Import.FMDL;

/// <summary>
/// Structure representing a geometry header for the FMDL 3D model format.
/// This is always the second header in an FMDL resource file.
/// </summary>
public struct FMdlGeometryHeader : IValidated, IImportableData<FMdlGeometryHeader>
{
	#region Fields

	// Numbers & Format:
	public uint vertexCount;
	public uint indexCount;
	public IndexFormat indexFormat;

	// Data flags:
	public bool hasExtendedVertexData;
	//...

	#endregion
	#region Constants

	/// <summary>
	/// The total size of this header, in bytes.
	/// </summary>
	public const int byteSize =
		2 * sizeof(uint) +
		1 * sizeof(IndexFormat) +
		1 * sizeof(bool);

	#endregion
	#region Methods

	public readonly bool IsValid()
	{
		bool isValid =
			vertexCount <= ushort.MaxValue || indexFormat == IndexFormat.UInt32;
		return isValid;
	}

	/// <summary>
	/// Calculates the total size of all vertex and index data.
	/// </summary>
	/// <returns>The total geometry size, in bytes.</returns>
	public readonly uint CalculateTotalDataSize()
	{
		uint totalDataByteSize = CalculateVertexDataSize() + CalculateIndexDataSize();
		return totalDataByteSize;
	}

	/// <summary>
	/// Calculates the total size of all vertex data.
	/// </summary>
	/// <returns>The total vertex data size, in bytes.</returns>
	public readonly uint CalculateVertexDataSize()
	{
		uint perVertexByteSize = BasicVertex.byteSize;
		if (hasExtendedVertexData)
		{
			perVertexByteSize += ExtendedVertex.byteSize;
		}

		uint totalVertexByteSize = perVertexByteSize * vertexCount;
		return totalVertexByteSize;
	}

	/// <summary>
	/// Calculates the total size of all index data.
	/// </summary>
	/// <returns>The total index data size, in bytes.</returns>
	public readonly uint CalculateIndexDataSize()
	{
		int indexByteSize = indexFormat == IndexFormat.UInt16
			? sizeof(ushort)
			: sizeof(int);

		uint totalIndexByteSize = (uint)indexByteSize * indexCount;
		return totalIndexByteSize;
	}

	public static bool Read(BinaryReader _reader, ILogger? _logger, out FMdlGeometryHeader _outData)
	{
		ArgumentNullException.ThrowIfNull(_reader);
		ObjectDisposedException.ThrowIf(_logger is not null && _logger.IsDisposed, _logger!);

		try
		{
			// Numbers & Format:
			uint vertexCount = _reader.ReadUInt32();
			uint indexCount = _reader.ReadUInt32();
			IndexFormat indexFormat = (IndexFormat)_reader.ReadByte();

			// Data flags:
			bool hasExtendedData = _reader.ReadBoolean();

			// Assemble header structure:
			_outData = new()
			{
				vertexCount = vertexCount,
				indexCount = indexCount,
				indexFormat = indexFormat,

				hasExtendedVertexData = hasExtendedData
			};
			return true;
		}
		catch (Exception ex)
		{
			_logger?.LogException("Failed to read FMDL geometry header!", ex, LogEntrySeverity.Normal);
			_outData = default;
			return false;
		}
	}

	public readonly bool Write(BinaryWriter _writer, ILogger? _logger)
	{
		ArgumentNullException.ThrowIfNull(_writer);
		ObjectDisposedException.ThrowIf(_logger is not null && _logger.IsDisposed, _logger!);

		if (!IsValid())
		{
			_logger?.LogError("Cannot write invalid FMDL geometry header to stream!");
			return false;
		}

		try
		{
			// Numbers & Format:
			_writer.Write(vertexCount);
			_writer.Write(indexCount);
			_writer.Write((byte)indexFormat);

			// Data flags:
			_writer.Write(hasExtendedVertexData);
			//...

			return true;
		}
		catch (Exception ex)
		{
			_logger?.LogException("Failed to write FMDL geometry header!", ex, LogEntrySeverity.Normal);
			return false;
		}
	}

	#endregion
}
