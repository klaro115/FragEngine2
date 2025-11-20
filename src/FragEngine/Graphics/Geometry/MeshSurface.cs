using FragEngine.Interfaces;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// A container for geometry buffers for a polygonal mesh surface.
/// </summary>
/// <param name="_graphicsService">The engine's graphics service singleton.</param>
/// <param name="_logger">The engine's logging service singleton.</param>
public sealed class MeshSurface(GraphicsService _graphicsService, ILogger _logger) : IExtendedDisposable, IValidated
{
	#region Fields

	private readonly GraphicsService graphicsService = _graphicsService;
	private readonly ILogger logger = _logger;

	private DeviceBuffer? bufVerticesBasic = null;
	private DeviceBuffer? bufVerticesExt = null;
	private DeviceBuffer? bufIndices = null;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

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
	/// Gets the primary vertex buffer containing basic vertex surface data of type <see cref="BasicVertex"/>.
	/// </summary>
	public DeviceBuffer? BufVerticesBasic => bufVerticesBasic;
	/// <summary>
	/// Gets a secondary vertex buffer containing extended vertex surface data of type <see cref="ExtendedVertex"/>.
	/// This buffer is optional and may be null if the mesh only has basic vertex data.
	/// </summary>
	public DeviceBuffer? BufVerticesExt => bufVerticesExt;
	/// <summary>
	/// Gets the index buffer, containing either 16-bit or 32-bit triangle indices.
	/// </summary>
	public DeviceBuffer? BufIndices => bufIndices;

	/// <summary>
	/// Gets whether this mesh has a full set of extended vertex data.
	/// </summary>
	public bool HasExtendedVertexData => BufVerticesExt is not null;

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
		bool isValid =
			!IsDisposed &&
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
	/// <param name="_cmdList">Optional. A command list through which the GPU upload is scheduled.
	/// If null, the geometry data is instead uploaded immediately via the graphics device.</param>
	/// <returns>True if geometry data was successfully uploaded to GPU buffers, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Mesh surface data may not be null.</exception>
	/// <exception cref="ObjectDisposedException">This instance or the command list have been disposed.</exception>
	public bool SetData(in MeshSurfaceData _data, CommandList? _cmdList = null)
	{
		ArgumentNullException.ThrowIfNull(_data);
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		ObjectDisposedException.ThrowIf(_cmdList is not null && _cmdList.IsDisposed, _cmdList!);

		if (_data.VerticesBasic is null || (_data.Indices16 is null && _data.Indices32 is null))
		{
			logger.LogError("Cannot set data of surface mesh; basic vertex or index arrays were null!");
			return false;
		}

		// Create, resize, and update geometry buffers:
		UpdateOrResizeBuffer(_data.VerticesBasic, BasicVertex.byteSize, ref bufVerticesBasic, _cmdList);

		if (_data.HasExtendedVertexData)
		{
			UpdateOrResizeBuffer(_data.VerticesExt!, ExtendedVertex.byteSize, ref bufVerticesExt, _cmdList);
		}

		bool use16BitIndices = _data.IndexFormat == IndexFormat.UInt16;
		if (use16BitIndices)
		{
			UpdateOrResizeBuffer(_data.Indices16!, sizeof(ushort), ref bufIndices, _cmdList);
		}
		else
		{
			UpdateOrResizeBuffer(_data.Indices32!, sizeof(int), ref bufIndices, _cmdList);
		}

		// Update geometry counts:
		VertexCount = _data.VertexCount;
		IndexCount = _data.IndexCount;
		TriangleCount = IndexCount / 3;
		IndexFormat = _data.IndexFormat;

		return true;
	}

	private bool UpdateOrResizeBuffer<T>(in T[] _elements, int _elementByteSize, ref DeviceBuffer? _buffer, CommandList? _cmdList) where T : unmanaged
	{
		int requiredTotalByteSize = _elementByteSize * _elements.Length;
		if (_buffer is not null && !_buffer.IsDisposed && _buffer.SizeInBytes >= requiredTotalByteSize)
		{
			return true;
		}

		_buffer?.Dispose();

		try
		{
			BufferDescription desc = new((uint)requiredTotalByteSize, BufferUsage.VertexBuffer);

			_buffer = graphicsService.ResourceFactory.CreateBuffer(ref desc);
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create vertex or index buffer for surface mesh!", ex, LogEntrySeverity.Normal);
			return false;
		}

		if (_cmdList is not null)
		{
			_cmdList.UpdateBuffer(bufVerticesBasic, 0, _elements);
		}
		else
		{
			graphicsService.Device.UpdateBuffer(bufVerticesBasic, 0, _elements);
		}
		return true;
	}

	#endregion
}
