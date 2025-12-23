using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// Structure containing additional geometry data for a vertex of a 3D model.<para/>
/// This data is intended to extend the basic geometry of <see cref="BasicVertex"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = byteSize)]
public record struct ExtendedVertex
{
	#region Fields

	/// <summary>
	/// The tangent vector of a surface at this vertex.
	/// </summary>
	public Vector3 tangent;

	/// <summary>
	/// The binormal vector of a surface at this vertex.
	/// </summary>
	public Vector3 binormal;

	/// <summary>
	/// A second set of texture coordinates at this vertex.
	/// </summary>
	public Vector2 uv2;

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
	public static ExtendedVertex Zero => new()
	{
		tangent = Vector3.UnitZ,
		binormal = Vector3.UnitX,
		uv2 = Vector2.Zero,
	};

	/// <summary>
	/// Gets a description of the GPU-side vertex layout for this structure.
	/// </summary>
	public static VertexLayoutDescription LayoutDescription => new(byteSize,
		new VertexElementDescription(nameof(tangent), VertexElementFormat.Float3, VertexElementSemantic.Normal),
		new VertexElementDescription(nameof(binormal), VertexElementFormat.Float3, VertexElementSemantic.Normal),
		new VertexElementDescription(nameof(uv2), VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate));

	#endregion
	#region Constructors

	public ExtendedVertex(Vector3 _tangent, Vector3 _binormal, Vector2 _uv2)
	{
		tangent = _tangent;
		binormal = _binormal;
		uv2 = _uv2;
	}

	#endregion
	#region Methods

	public readonly override string ToString() => $"(Tangents: {tangent}; Binormals: {binormal}, UV2: {uv2})";

	#endregion
}
