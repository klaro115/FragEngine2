using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// A container for geometry buffers for a polygonal mesh surface.
/// </summary>
/// <param name="_graphicsService">The engine's graphics service singleton.</param>
/// <param name="_logger">The engine's logging service singleton.</param>
public sealed class MeshSurface(GraphicsService _graphicsService, ILogger _logger) : IExtendedDisposable, IValidated
{
	#region Types

	public enum GeometryBufferStatus
	{
		NoData,
		BufferAllocationPending,
		BuffersAllocated,
		DataUploadPending,
		DataUploaded,
	}

	#endregion
	#region Fields

	private readonly GraphicsService graphicsService = _graphicsService;
	private readonly ILogger logger = _logger;

	private DeviceBuffer? bufVerticesBasic = null;
	private DeviceBuffer? bufVerticesExt = null;
	private DeviceBuffer? bufIndices = null;

	private BasicVertex[]? pendingVerticesBasic = null;
	private ExtendedVertex[]? pendingVerticesExt = null;
	private ushort[]? pendingIndices16 = null;
	private int[]? pendingIndices32 = null;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets the current status of geometry buffers and mesh data.
	/// </summary>
	/// <remarks>
	/// Note: If data has been assigned already, the status will change to <see cref="GeometryBufferStatus.DataUploaded"/>
	/// at the latest when the method '<see cref="GetGeometryBuffers(out DeviceBuffer?, out DeviceBuffer?, out DeviceBuffer?, CommandList?)"/>'
	/// is first called.
	/// </remarks>
	public GeometryBufferStatus Status { get; private set; } = GeometryBufferStatus.NoData;

	/// <summary>
	/// Gets the number of vertices in the mesh.
	/// </summary>
	public int VertexCount { get; private set; } = 0;
	/// <summary>
	/// Gets the number of vertex indices in the mesh. Three indices make a triangular polygon face.
	/// </summary>
	public int IndexCount { get; private set; } = 0;
	/// <summary>
	/// Gets the number of triangular faces in the mesh.
	/// </summary>
	public int TriangleCount { get; private set; } = 0;
	/// <summary>
	/// Gets the index data format, i.e. whether the mesh uses 16-bit or 32-bit indices.
	/// </summary>
	public IndexFormat IndexFormat { get; private set; } = IndexFormat.UInt16;
	/// <summary>
	/// Gets the byte size of each index in the index buffer. This will be either 2 bytes
	/// (16-bit <see langword="ushort"/>) or 4 bytes (32-bit <see langword="int"/>).
	/// </summary>
	public uint IndexByteSize => IndexFormat == IndexFormat.UInt16 ? (uint)sizeof(ushort) : sizeof(int);
	/// <summary>
	/// Gets whether this mesh has a full set of extended vertex data.
	/// </summary>
	public bool HasExtendedVertexData { get; private set; } = false;

	#endregion
	#region Constructors

	~MeshSurface()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool _isDisposing)
	{
		IsDisposed = true;

		bufVerticesBasic?.Dispose();
		bufVerticesExt?.Dispose();
		bufIndices?.Dispose();

		if (_isDisposing)
		{
			bufVerticesBasic = null;
			bufVerticesExt = null;
			bufIndices = null;
		}
	}

	public bool IsValid()
	{
		if (IsDisposed || Status <= GeometryBufferStatus.BufferAllocationPending) return false;

		bool isValid;

		// If data is still awaiting upload:
		if (Status == GeometryBufferStatus.DataUploadPending)
		{
			isValid =
			pendingVerticesBasic is not null &&
			pendingVerticesBasic.Length >= VertexCount &&
			(pendingIndices16 is not null || pendingIndices32 is not null) &&
			(!HasExtendedVertexData || (pendingVerticesExt is not null && pendingVerticesExt!.Length >= VertexCount));
			return isValid;
		}

		// If buffers are allocated, or if data has finished uploading:
		isValid =
			bufVerticesBasic is not null &&
			bufIndices is not null &&
			!bufVerticesBasic.IsDisposed &&
			!bufIndices.IsDisposed &&
			(bufVerticesExt is null || !bufVerticesExt.IsDisposed);
		return isValid;
	}

	/// <summary>
	/// Tries to upload new geometry data to this mesh.
	/// </summary>
	/// <param name="_data">The surface data for this mesh, may not be null.</param>
	/// <param name="_allocateImmediately">Whether to create GPU-side geometry buffers immediately.
	/// If false, the vertex and index buffers will only be created upon the first call to
	/// '<see cref="GetGeometryBuffers(out DeviceBuffer?, out DeviceBuffer?, out DeviceBuffer?, CommandList?)"/>'.</param>
	/// <param name="_uploadImmediately">Whether to upload new geometry data to GPU-side buffers immedietaley.
	/// If false, the vertex and index buffers will only be populated from pending data upon the next call to
	/// '<see cref="GetGeometryBuffers(out DeviceBuffer?, out DeviceBuffer?, out DeviceBuffer?, CommandList?)"/>'.
	/// This parameter is always false if <paramref name="_allocateImmediately"/> is <see langword="false"/>.</param>
	/// <returns>True if geometry data was successfully uploaded to GPU buffers, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Mesh surface data may not be null.</exception>
	/// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
	public bool SetData(in MeshSurfaceData _data, bool _allocateImmediately = true, bool _uploadImmediately = true)
	{
		ArgumentNullException.ThrowIfNull(_data);
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		if (_data.VerticesBasic is null || (_data.Indices16 is null && _data.Indices32 is null))
		{
			logger.LogError($"Cannot set data of {nameof(MeshSurface)}; basic vertex or index arrays were null!");
			return false;
		}

		_uploadImmediately &= _allocateImmediately;
		Status = GeometryBufferStatus.BufferAllocationPending;

		// Select or cache new geometry data:
		bool use16BitIndices = _data.IndexFormat == IndexFormat.UInt16;
		if (_uploadImmediately)
		{
			pendingVerticesBasic = _data.VerticesBasic;
			pendingVerticesExt = _data.VerticesExt;
			pendingIndices16 = _data.Indices16;
			pendingIndices32 = _data.Indices32;
		}
		else
		{
			pendingVerticesBasic = _data.VerticesBasic[.._data.VertexCount];
			pendingVerticesExt = _data.HasExtendedVertexData ? _data.VerticesExt![.._data.VertexCount] : null;
			pendingIndices16 = use16BitIndices ? _data.Indices16![.._data.IndexCount] : null;
			pendingIndices32 = !use16BitIndices ? _data.Indices32![.._data.IndexCount] : null;
		}

		// Create or resize geometry buffers:
		if (_allocateImmediately && !CreateOrResizeAllBuffers(_data.VertexCount, _data.IndexCount, _data.IndexByteSize, _data.HasExtendedVertexData))
		{
			logger.LogError($"Failed to create geometry buffers for {nameof(MeshSurface)}! ({nameof(SetData)})");
			return false;
		}

		// Update geometry counts:
		VertexCount = _data.VertexCount;
		IndexCount = _data.IndexCount;
		TriangleCount = IndexCount / 3;
		IndexFormat = _data.IndexFormat;
		HasExtendedVertexData = _data.HasExtendedVertexData;

		// If requested, upload data immediately:
		if (_uploadImmediately && !UploadPendingData(null))
		{
			logger.LogError($"Failed to upload geometry data of {nameof(MeshSurface)} to GPU buffer! ({nameof(SetData)})");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Gets and prepares geometry buffers for immediate use in draw calls.
	/// </summary>
	/// <remarks>
	/// Note: If geometry buffers have not been allocated yet, this will be done immediately.
	/// If geometry data has not been uploaded to GPU-side buffers yet, this will happen before the method returns.
	/// </remarks>
	/// <param name="_outBufVerticesBasic">Outputs the primary vertex buffer containing basic vertex surface data of type <see cref="BasicVertex"/>.</param>
	/// <param name="_outBufVerticesExt">Outputs gets a secondary vertex buffer containing extended vertex surface data of type <see cref="ExtendedVertex"/>.
	/// This buffer is optional and may be null if the mesh only has basic vertex data. Check '<see cref="HasExtendedVertexData"/>' to see if extended data
	/// is available.</param>
	/// <param name="_outBufIndices">Outputs the index buffer, containing either 16-bit or 32-bit triangle indices.</param>
	/// <param name="_cmdList">Optional. A command list through which the GPU upload is scheduled. If null, the geometry data is instead uploaded immediately
	/// via the graphics device.</param>
	/// <returns>True if geometry buffers are ready for use, false otherwise.</returns>
	public bool GetGeometryBuffers([NotNullWhen(true)] out DeviceBuffer? _outBufVerticesBasic, out DeviceBuffer? _outBufVerticesExt, [NotNullWhen(true)] out DeviceBuffer? _outBufIndices, CommandList? _cmdList = null)
	{
		if (IsDisposed)
		{
			logger.LogError($"Cannot get geometry buffer of {nameof(MeshSurface)} that has already been disposed!");
			goto abort;
		}

		// Ensure buffers are allocated and populated:
		if (Status != GeometryBufferStatus.DataUploaded)
		{
			if (Status <= GeometryBufferStatus.BufferAllocationPending && !CreateOrResizeAllBuffers(VertexCount, IndexCount, IndexByteSize, HasExtendedVertexData))
			{
				logger.LogError($"Failed to create geometry buffers for {nameof(MeshSurface)}! ({nameof(GetGeometryBuffers)})");
				goto abort;
			}
			if (Status <= GeometryBufferStatus.DataUploadPending && !UploadPendingData(_cmdList))
			{
				logger.LogError($"Failed to upload geometry data of {nameof(MeshSurface)} to GPU buffer! ({nameof(GetGeometryBuffers)})");
				goto abort;
			}
		}

		// Output buffers and return success:
		_outBufVerticesBasic = bufVerticesBasic!;
		_outBufVerticesExt = bufVerticesExt;
		_outBufIndices = bufIndices!;
		return true;

	abort:
		_outBufVerticesBasic = null;
		_outBufVerticesExt = null;
		_outBufIndices = null;
		return false;
	}

	private bool CreateOrResizeAllBuffers(int _vertexCount, int _indexCount, uint _indexByteSize, bool _hasExtendedData)
	{
		bool success = true;

		// Create vertex buffers:
		success &= CreateOrResizeBuffer(_vertexCount, BasicVertex.byteSize, BufferUsage.VertexBuffer, ref bufVerticesBasic);

		if (_hasExtendedData)
		{
			success &= CreateOrResizeBuffer(_vertexCount, ExtendedVertex.byteSize, BufferUsage.VertexBuffer, ref bufVerticesExt);
		}

		// Create index buffer:
		success &= CreateOrResizeBuffer(_indexCount, (int)_indexByteSize, BufferUsage.IndexBuffer, ref bufIndices);

		// Update status:
		if (success)
		{
			Status = pendingVerticesBasic is null && pendingVerticesExt is null && pendingIndices16 is null && pendingIndices32 is null
				? GeometryBufferStatus.BuffersAllocated
				: GeometryBufferStatus.DataUploadPending;
		}
		else
		{
			Status = GeometryBufferStatus.BufferAllocationPending;
		}
		return success;
	}

	private bool CreateOrResizeBuffer(int _elementCount, int _elementByteSize, BufferUsage _usage, ref DeviceBuffer? _buffer)
	{
		int requiredTotalByteSize = _elementByteSize * _elementCount;
		if (_buffer is not null && !_buffer.IsDisposed && _buffer.SizeInBytes >= requiredTotalByteSize)
		{
			return true;
		}

		_buffer?.Dispose();

		try
		{
			BufferDescription desc = new((uint)requiredTotalByteSize, _usage);

			_buffer = graphicsService.ResourceFactory.CreateBuffer(ref desc);
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create vertex or index buffer for {nameof(MeshSurface)}!", ex, LogEntrySeverity.Normal);
			return false;
		}

		return true;
	}

	private bool UploadPendingData(CommandList? _cmdList)
	{
		bool success = true;

		try
		{
			// Upload vertex data:
			if (pendingVerticesBasic is not null)
			{
				success &= UploadBufferData(in pendingVerticesBasic, bufVerticesBasic, _cmdList);
				pendingVerticesBasic = null;
			}
			if (HasExtendedVertexData && pendingVerticesExt is not null)
			{
				success &= UploadBufferData(in pendingVerticesExt, bufVerticesExt, _cmdList);
				pendingVerticesExt = null;
			}

			// Upload index data:
			if (pendingIndices16 is not null)
			{
				success &= UploadBufferData(in pendingIndices16, bufIndices, _cmdList);
				pendingIndices16 = null;
			}
			else if (pendingIndices32 is not null)
			{
				success &= UploadBufferData(in pendingIndices32, bufIndices, _cmdList);
				pendingIndices32 = null;
			}
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to upload geometry data!", ex);
			Status = GeometryBufferStatus.DataUploadPending;
			return false;
		}

		Status = GeometryBufferStatus.DataUploaded;
		return success;
	}

	private bool UploadBufferData<T>(in T[] _elements, DeviceBuffer? _buffer, CommandList? _cmdList) where T : unmanaged
	{
		if (_cmdList is not null)
		{
			_cmdList.UpdateBuffer(_buffer, 0, _elements);
		}
		else
		{
			graphicsService.Device.UpdateBuffer(_buffer, 0, _elements);
		}
		return true;
	}

	#endregion
}
