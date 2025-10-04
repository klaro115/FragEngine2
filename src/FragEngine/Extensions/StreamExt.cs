using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="Stream"/> class.
/// </summary>
public static class StreamExt
{
	#region Methods

	/// <summary>
	/// Reads an unmaned struct from a binary data stream.
	/// </summary>
	/// <typeparam name="T">The type of the struct, must be unmanaged.<para/>
	/// It is recommended to add the <see cref="StructLayoutAttribute"/> to the type, with a sequential or explicit layout.</typeparam>
	/// <param name="_stream">This stream, must support reading.</param>
	/// <param name="_structByteSize">The size of the struct, in bytes. Must be at least 1 byte.</param>
	/// <returns>A struct that was read from stream.</returns>
	/// <exception cref="ArgumentException">Struct size may not be zero or negative.</exception>
	/// <exception cref="ArgumentNullException">Stream may not be null.</exception>
	/// <exception cref="EndOfStreamException">Attempting to read past end of stream.</exception>
	public static unsafe T ReadStruct<T>(this Stream _stream, int _structByteSize) where T : unmanaged
	{
		ArgumentNullException.ThrowIfNull(_stream);

		if (_structByteSize < 1)
		{
			throw new ArgumentException("Struct size must be at least 1 byte!", nameof(_structByteSize));
		}
		Debug.Assert(_stream.CanRead, "Stream must support reading!");

		T data;
		Span<byte> buffer = stackalloc byte[_structByteSize];

		_stream.ReadExactly(buffer);

		fixed (byte* pBuffer = buffer)
		{
			data = *(T*)pBuffer;
		}
		return data;
	}

	/// <summary>
	/// Writes an unmanaged struct to a binary data stream.
	/// </summary>
	/// <typeparam name="T">The type of the struct, must be unmanaged.<para/>
	/// It is recommended to add the <see cref="StructLayoutAttribute"/> to the type, with a sequential or explicit layout.</typeparam>
	/// <param name="_stream">This stream, must support writing.</param>
	/// <param name="_structData">The size of the struct, in bytes. Must be at least 1 byte.</param>
	/// <param name="_structByteSize"></param>
	/// <exception cref="ArgumentException">Struct size may not be zero or negative.</exception>
	/// <exception cref="ArgumentNullException">Stream may not be null.</exception>
	public static unsafe void WriteStruct<T>(this Stream _stream, T _structData, int _structByteSize) where T : unmanaged
	{
		ArgumentNullException.ThrowIfNull(_stream);

		if ( _structByteSize < 1)
		{
			throw new ArgumentException("Struct size must be at least 1 byte!", nameof(_structByteSize));
		}
		Debug.Assert(_stream.CanWrite, "Stream must support writing!");

		Span<byte> buffer = stackalloc byte[_structByteSize];
		fixed (byte* pBuffer = buffer)
		{
			*(T*)pBuffer = _structData;
		}

		_stream.Write(buffer);
	}

	#endregion
}
