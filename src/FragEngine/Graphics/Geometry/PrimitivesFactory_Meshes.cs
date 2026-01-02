using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

// NOTE: This partial class implements the creation of meshes with GPU-side buffers. Main methods are `Create[Shape]Mesh`.

public partial class PrimitivesFactory
{
	#region Methods

	/// <summary>
	/// Creates a cube mesh.
	/// </summary>
	/// <remarks>
	/// The cube mesh will be centered on the coordinate origin, with its maximum extents stretching the same in all directions.
	/// </remarks>
	/// <param name="_size">The dimensions of the cube.</param>
	/// <param name="_outMesh">Outputs the newly created mesh, or null, if mesh creation failed.</param>
	/// <param name="_cmdList">Optional. A command list through which the GPU upload is scheduled.
	/// If null, the geometry data is instead uploaded immediately via the graphics device.
	/// If non-null, creation of GPU-side geometry buffers and upload of data to the GPU will be queued up via the command list.</param>
	/// <param name="_createExtendedVertexData">Whether to also generate extended vertex data for this mesh.</param>
	/// <returns>True if the mesh was created successfully, false otherwise.</returns>
	public bool CreateCubeMesh(in Vector3 _size, [NotNullWhen(true)] out MeshSurface? _outMesh, CommandList? _cmdList = null, bool _createExtendedVertexData = true)
	{
		MeshSurfaceData data = CreateCubeData(in _size, _createExtendedVertexData);
		return CreateMeshFromSurfaceData(data, _cmdList, out _outMesh);
	}

	//... (add more primitive shapes here)

	private bool CreateMeshFromSurfaceData(MeshSurfaceData _data, CommandList? _cmdList, [NotNullWhen(true)] out MeshSurface? _outMesh)
	{
		ArgumentNullException.ThrowIfNull(_data);
		ObjectDisposedException.ThrowIf(_cmdList is not null && _cmdList.IsDisposed, _cmdList!);

		if (!_data.IsValid())
		{
			logger.LogError("Cannot create surface mesh from invalid data!");
			_outMesh = null;
			return false;
		}

		// Create empty mesh instance:
		try
		{
			_outMesh = serviceProvider.GetRequiredService<MeshSurface>();
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create mesh surface instance!", ex, Logging.LogEntrySeverity.Normal);
			_outMesh = null;
			return false;
		}

		// Populate mesh:
		bool uploadImmediately = _cmdList is null;
		if (!_outMesh.SetData(in _data, uploadImmediately, uploadImmediately))
		{
			return false;
		}
		if (!uploadImmediately && !_outMesh.GetGeometryBuffers(out _, out _, out _, _cmdList))
		{
			return false;
		}

		return true;
	}

	#endregion
}
