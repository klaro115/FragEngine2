using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Interfaces;

namespace FragEngine.Graphics.Geometry.Import.FMDL;

/// <summary>
/// Structure representing a file header for the FMDL 3D model format.
/// This is always the first piece of data in an FMDL resource file.
/// </summary>
public struct FMdlFileHeader : IValidated, IImportableData<FMdlFileHeader>, IExportableData
{
	#region Fields

	//GENERAL:

	public uint magicNumbers;
	public uint fileSize;
	public byte versionMajor;
	public byte versionMinor;

	// HEADERS:

	public FMdlHeaderFlags headerFlags;
	public ushort headerOffset;
	public ushort totalHeaderSize;

	// DATA:

	public ushort dataOffset;
	public uint totalDataSize;

	#endregion
	#region Constants

	/// <summary>
	/// The total size of this header, in bytes.
	/// </summary>
	public const int byteSize =
		3 * sizeof(uint) +
		2 * sizeof(byte) +
		1 * sizeof(FMdlHeaderFlags) +
		3 * sizeof(ushort);

	private const int minimumTotalHeaderSize =
		byteSize +
		FMdlGeometryHeader.byteSize;

	#endregion
	#region Methods

	public readonly bool IsValid()
	{
		bool isValid =
			magicNumbers == FMdlConstants.magicNumbers &&
			headerFlags.HasFlag(FMdlConstants.mandatoryHeaderFlags) &&
			headerOffset >= 0 &&
			totalHeaderSize >= minimumTotalHeaderSize &&
			dataOffset >= headerOffset + totalHeaderSize &&
			fileSize >= headerOffset + totalHeaderSize + totalDataSize;
		return isValid;
	}

	/// <summary>
	/// Gets the total number of headers in this file, including this file header.
	/// </summary>
	/// <remarks>
	/// The file header and geometry header are mandatory and always the first headers
	/// within an FMDL file. If a file has fewer than 2 headers, it is guaranteed to be
	/// invalid.
	/// </remarks>
	/// <returns>The number of headers.</returns>
	public readonly int GetHeaderCount()
	{
		int headerCount = 0;

		for (int i = 0; i < 16; ++i)
		{
			FMdlHeaderFlags flag = (FMdlHeaderFlags)(i << i);
			if (headerFlags.HasFlag(flag))
			{
				headerCount++;
			}
		}

		return headerCount;
	}

	/// <summary>
	/// Tries to read the FMDL file header from stream.
	/// </summary>
	/// <param name="_reader">A binary reader that reads from a resource stream.</param>
	/// <param name="_logger">Optional. A logger for outputting errors.</param>
	/// <param name="_outFileHeader">Outputs the fully read and parsed file header.</param>
	/// <returns>True if the header was read successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Binary reader may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Logger has already been disposed.</exception>
	public static bool Read(BinaryReader _reader, ILogger? _logger, out FMdlFileHeader _outFileHeader)
	{
		ArgumentNullException.ThrowIfNull(_reader);
		ObjectDisposedException.ThrowIf(_logger is not null && _logger.IsDisposed, _logger!);

		try
		{
			// General:
			uint magicNumber = _reader.ReadUInt32();
			if (magicNumber != FMdlConstants.magicNumbers)
			{
				_logger?.LogError("Unexpected magic numbers for FMDL file header!");
				_outFileHeader = default;
				return false;
			}

			uint fileSize = _reader.ReadUInt32();
			if (fileSize < minimumTotalHeaderSize)
			{
				_logger?.LogError("Insufficient total file size in FMDL file header!");
				_outFileHeader = default;
				return false;
			}

			// Format versions:
			byte versionMajor = _reader.ReadByte();
			byte versionMinor = _reader.ReadByte();
			if (versionMajor < FMdlConstants.minimumVersionMajor)
			{
				_logger?.LogError($"Unsupported deprecated major version of FMDL format: {versionMajor} < {FMdlConstants.minimumVersionMajor}");
				_outFileHeader = default;
				return false;
			}
			if (versionMajor == FMdlConstants.minimumVersionMajor &&
				versionMinor < FMdlConstants.minimumVersionMinor)
			{
				_logger?.LogError($"Unsupported deprecated minor version of FMDL format: {versionMinor} < {FMdlConstants.minimumVersionMinor}");
				_outFileHeader = default;
				return false;
			}

			// Headers:
			FMdlHeaderFlags headerFlags = (FMdlHeaderFlags)_reader.ReadUInt16();
			if (!headerFlags.HasFlag(FMdlConstants.mandatoryHeaderFlags))
			{
				_logger?.LogError("Incomplete headers in FMDL file!");
				_outFileHeader = default;
				return false;
			}

			ushort headerOffset = _reader.ReadUInt16();
			ushort totalHeaderSize = _reader.ReadUInt16();

			// Data:
			ushort dataOffset = _reader.ReadUInt16();
			uint totalDataSize = _reader.ReadUInt32();

			// Assemble header structure:
			_outFileHeader = new()
			{
				magicNumbers = FMdlConstants.magicNumbers,
				fileSize = fileSize,
				versionMajor = versionMajor,
				versionMinor = versionMinor,

				headerFlags = headerFlags,
				headerOffset = headerOffset,
				totalHeaderSize = totalHeaderSize,

				dataOffset = dataOffset,
				totalDataSize = totalDataSize
			};
			if (!_outFileHeader.IsValid())
			{
				_logger?.LogError("File header of FMDL file is invalid!");
				return false;
			}

			return true;
		}
		catch (Exception ex)
		{
			_logger?.LogException("Failed to read FMDL file header!", ex, LogEntrySeverity.Normal);
			_outFileHeader = default;
			return false;
		}
	}

	/// <summary>
	/// Tries to write the FMDL file header to stream.
	/// </summary>
	/// <param name="_writer">A binary writer that writes to a resource stream.</param>
	/// <param name="_logger">Optional. A logger for outputting errors.</param>
	/// <returns>True if the header was written successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Binary writer may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Logger has already been disposed.</exception>
	public readonly bool Write(BinaryWriter _writer, ILogger? _logger)
	{
		ArgumentNullException.ThrowIfNull(_writer);
		ObjectDisposedException.ThrowIf(_logger is not null && _logger.IsDisposed, _logger!);

		if (!IsValid())
		{
			_logger?.LogError("Cannot write invalid FMDL file header to stream!");
			return false;
		}

		try
		{
			// General:
			_writer.Write(FMdlConstants.magicNumbers);
			_writer.Write(fileSize);
			_writer.Write(FMdlConstants.currentVersionMajor);
			_writer.Write(FMdlConstants.currentVersionMinor);

			// Headers:
			_writer.Write((ushort)headerFlags);
			_writer.Write(headerOffset);
			_writer.Write(totalHeaderSize);

			// Data:
			_writer.Write(dataOffset);
			_writer.Write(totalDataSize);

			return true;
		}
		catch (Exception ex)
		{
			_logger?.LogException("Failed to write FMDL file header!", ex, LogEntrySeverity.Normal);
			return false;
		}
	}

	#endregion
}
