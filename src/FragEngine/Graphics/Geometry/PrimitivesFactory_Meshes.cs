using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

public partial class PrimitivesFactory
{
	#region Methods

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
		return _outMesh.SetData(in _data, _cmdList);
	}

	#endregion
}
