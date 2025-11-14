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
	/// Reads an unmanaged struct from a binary data stream.
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
	/// Reads an array of unmanaged structs from a binary data stream.
	/// </summary>
	/// <typeparam name="T">The type of the struct, must be unmanaged.<para/>
	/// It is recommended to add the <see cref="StructLayoutAttribute"/> to the type, with a sequential or explicit layout.</typeparam>
	/// <param name="_stream">This stream, must support reading.</param>
	/// <param name="_structCount">The number of structs to write to stream. Must be non-negative.</param>
	/// <param name="_structByteSize">The size of the struct, in bytes. Must be at least 1 byte.</param>
	/// <returns>A struct that was read from stream.</returns>
	/// <exception cref="ArgumentException">Struct size may not be zero or negative.</exception>
	/// <exception cref="ArgumentNullException">Stream may not be null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Struct count was negative.</exception>
	/// <exception cref="EndOfStreamException">Attempting to read past end of stream.</exception>
	public static unsafe T[] ReadStructs<T>(this Stream _stream, int _structCount, int _structByteSize) where T : unmanaged
	{
		ArgumentNullException.ThrowIfNull(_stream);

		if (_structCount < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(_structCount), "Struct count may not be negative!");
		}
		if (_structByteSize < 1)
		{
			throw new ArgumentException("Struct size must be at least 1 byte!", nameof(_structByteSize));
		}
		Debug.Assert(_stream.CanRead, "Stream must support reading!");

		int totalByteSize = _structByteSize * _structCount;

		T[] data = new T[_structCount];

		fixed (T* pData = data)
		{
			Span<byte> dstBuffer = new(pData, totalByteSize);
			_stream.ReadExactly(dstBuffer);
		}

		return data;
	}

	/// <summary>
	/// Writes an unmanaged struct to a binary data stream.
	/// </summary>
	/// <typeparam name="T">The type of the struct, must be unmanaged.<para/>
	/// It is recommended to add the <see cref="StructLayoutAttribute"/> to the type, with a sequential or explicit layout.</typeparam>
	/// <param name="_stream">This stream, must support writing.</param>
	/// <param name="_structData">The struct we wish to write to stream.</param>
	/// <param name="_structByteSize">The size of the struct, in bytes. Must be at least 1 byte.</param>
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

	/// <summary>
	/// Writes an array of unmanaged structs
	/// </summary>
	/// <typeparam name="T">The type of the struct, must be unmanaged.<para/>
	/// It is recommended to add the <see cref="StructLayoutAttribute"/> to the type, with a sequential or explicit layout.</typeparam>
	/// <param name="_stream">This stream, must support writing.</param>
	/// <param name="_structsData">An array of structs we wish to write to stream.</param>
	/// <param name="_structCount">The number of structs to write to stream. Must be less than or equal to the length of the arrray.</param>
	/// <param name="_structByteSize">The size of the struct, in bytes. Must be at least 1 byte.</param>
	/// <exception cref="ArgumentException">Struct size may not be zero or negative.</exception>
	/// <exception cref="ArgumentNullException">Stream may not be null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Struct count was out of bounds of the source array.</exception>
	public static unsafe void WriteStructs<T>(this Stream _stream, T[] _structsData, int _structCount, int _structByteSize) where T : unmanaged
	{
		ArgumentNullException.ThrowIfNull(_stream);
		ArgumentNullException.ThrowIfNull(_structsData);

		if (_structCount < 0 || _structCount > _structsData.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(_structCount), "Struct count was out of bounds of the source array!");
		}
		if (_structByteSize < 1)
		{
			throw new ArgumentException("Struct size must be at least 1 byte!", nameof(_structByteSize));
		}
		Debug.Assert(_stream.CanWrite, "Stream must support writing!");

		int totalByteSize = _structByteSize * _structCount;

		Span<byte> buffer = stackalloc byte[totalByteSize];
		fixed (byte* pBuffer = buffer)
		{
			for (int i = 0; i < _structCount; ++i)
			{
				*(T*)(pBuffer + i * _structByteSize) = _structsData[i];
			}
		}

		_stream.Write(buffer);
	}

	#endregion
}
