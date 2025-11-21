using FragEngine.Logging;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

// NOTE: This partial class implements the creation of geometry data on the CPU side. Main methods are `Create[Shape]Data`.

/// <summary>
/// Factory service for creating 3D models of primitive shapes.
/// </summary>
/// <param name="_serviceProvider">The engine's service provider, used for instantiating meshes.</param>
/// <param name="_logger">The engine's logging service singleton.</param>
public partial class PrimitivesFactory(IServiceProvider _serviceProvider, ILogger _logger)
{
	#region Fields

	private readonly IServiceProvider serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

	#endregion
	#region Methods

	/// <summary>
	/// Creates the surface geometry data for a cube.
	/// </summary>
	/// <remarks>
	/// The cube mesh will be centered on the coordinate origin, with its maximum extents stretching the same in all directions.
	/// </remarks>
	/// <param name="_size">The dimensions of the cube.</param>
	/// <param name="_createExtendedVertexData">Whether to also generate extended vertex data for this mesh.</param>
	/// <returns>The mesh data.</returns>
	public MeshSurfaceData CreateCubeData(in Vector3 _size, bool _createExtendedVertexData = true)
	{
		float x = _size.X * 0.5f;
		float y = _size.Y * 0.5f;
		float z = _size.Z * 0.5f;

		// Define geometry:
		Span<BasicVertex> vertsBasic =
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
		Span<ushort> indices =
		[
			// Left:
			0, 1, 2,
			1, 3, 2,

			//TODO
		];

		Span<ExtendedVertex> vertsExt = _createExtendedVertexData ? stackalloc ExtendedVertex[vertsBasic.Length] : [];

		if (_createExtendedVertexData)
		{
			vertsExt =
			[
				// TODO
			];
		}

		// Create and populate data object:
		MeshSurfaceData data = new();

		data.SetVertices(vertsBasic, vertsExt, vertsBasic.Length, logger);
		data.SetIndices16(indices, indices.Length, IndexFormat.UInt16, logger);

		return data;
	}

	//... (add more primitive shapes here)

	#endregion
}
