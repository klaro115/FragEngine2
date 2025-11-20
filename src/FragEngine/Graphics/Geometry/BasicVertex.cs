using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// Structure containing basic geometry data for a vertex of a 3D model.<para/>
/// This data should suffice for rendering a model with very basic shading.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = byteSize)]
public record struct BasicVertex
{
	#region Fields

	/// <summary>
	/// The position of the vertex.
	/// </summary>
	public Vector3 position;

	/// <summary>
	/// The normal vector of a surface at this vertex.
	/// </summary>
	public Vector3 normal;

	/// <summary>
	/// The texture coordinates at this vertex.
	/// </summary>
	public Vector2 uvs;

	#endregion
	#region Constants

	/// <summary>
	/// The size of a vertex, in bytes.
	/// </summary>
	public const int byteSize = (3 + 3 + 2) * sizeof(float);    // = 32 bytes

	#endregion
	#region Properties

	/// <summary>
	/// Gets a vertex with all-zero coordinates, and default normals.
	/// </summary>
	public static BasicVertex Zero => new()
	{
		position = Vector3.Zero,
		normal = Vector3.UnitY,
		uvs = Vector2.Zero,
	};

	/// <summary>
	/// Gets a description of the GPU-side vertex layout for this structure.
	/// </summary>
	public static VertexLayoutDescription LayoutDescription => new(byteSize,
		new VertexElementDescription(nameof(position), VertexElementFormat.Float3, VertexElementSemantic.Position),
		new VertexElementDescription(nameof(normal), VertexElementFormat.Float3, VertexElementSemantic.Normal),
		new VertexElementDescription(nameof(uvs), VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate));

	#endregion
	#region Constructors

	public BasicVertex(Vector3 _position, Vector3 _normal, Vector2 _uvs)
	{
		position = _position;
		normal = _normal;
		uvs = _uvs;
	}

	#endregion
	#region Methods

	public readonly override string ToString() => $"(Position: {position}; Normals: {normal}, UVs: {uvs})";

	#endregion
}
