using FragEngine.Logging;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

public partial class PrimitivesFactory(IServiceProvider _serviceProvider, ILogger _logger)
{
	#region Fields

	private readonly IServiceProvider serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

	#endregion
	#region Methods

	public MeshSurfaceData CreateCubeData(in Vector3 _size, bool _createExtendedVertexData = true)
	{
		float x = _size.X * 0.5f;
		float y = _size.Y * 0.5f;
		float z = _size.Z * 0.5f;

		BasicVertex[] vertsBasic =
		[
			// Left:
			new(new(-x, -y, -z), -Vector3.UnitX, new(1, 0)),
			new(new(-x, -y,  z), -Vector3.UnitX, new(0, 0)),
			new(new(-x,  y, -z), -Vector3.UnitX, new(1, 1)),
			new(new(-x,  y,  z), -Vector3.UnitX, new(0, 1)),
			// Back:
			new(new(-x, -y, -z), -Vector3.UnitZ, new(0, 0)),
			new(new( x, -y, -z), -Vector3.UnitZ, new(1, 0)),
			new(new(-x,  y, -z), -Vector3.UnitZ, new(0, 1)),
			new(new( x,  y, -z), -Vector3.UnitZ, new(1, 1)),
			// Right:
			new(new( x, -y, -z),  Vector3.UnitX, new(0, 0)),
			new(new( x, -y,  z),  Vector3.UnitX, new(1, 0)),
			new(new( x,  y, -z),  Vector3.UnitX, new(0, 1)),
			new(new( x,  y,  z),  Vector3.UnitX, new(1, 1)),
			// Front:
			new(new(-x, -y,  z),  Vector3.UnitZ, new(1, 0)),
			new(new( x, -y,  z),  Vector3.UnitZ, new(0, 0)),
			new(new(-x,  y,  z),  Vector3.UnitZ, new(1, 1)),
			new(new( x,  y,  z),  Vector3.UnitZ, new(0, 1)),
			// Bottom:
			new(new(-x, -y, -z), -Vector3.UnitY, new(1, 1)),
			new(new( x, -y, -z), -Vector3.UnitY, new(0, 1)),
			new(new(-x, -y,  z), -Vector3.UnitY, new(1, 0)),
			new(new( x, -y,  z), -Vector3.UnitY, new(0, 0)),
			// Top:
			new(new(-x,  y, -z),  Vector3.UnitY, new(0, 0)),
			new(new( x,  y, -z),  Vector3.UnitY, new(1, 0)),
			new(new(-x,  y,  z),  Vector3.UnitY, new(0, 1)),
			new(new( x,  y,  z),  Vector3.UnitY, new(1, 1)),
		];
		ushort[] indices =
		[
			// Left:
			0, 1, 2,
			1, 3, 2,

			//TODO
		];

		ExtendedVertex[]? vertsExt = null;

		if (_createExtendedVertexData)
		{
			vertsExt =
			[
				// TODO
			];
		}

		MeshSurfaceData data = new();

		data.SetVertices(vertsBasic, vertsExt, vertsBasic.Length, logger);
		data.SetIndices16(indices, indices.Length, IndexFormat.UInt16, logger);

		return data;
	}

	//... (add more primitive shapes here)

	#endregion
}
