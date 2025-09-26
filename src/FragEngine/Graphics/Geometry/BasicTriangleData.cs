using FragEngine.Interfaces;
using System.Numerics;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// A structure containing the 3 vertex indices of a triangular polygon.
/// </summary>
/// <param name="IndexA">The 1st vertex index.</param>
/// <param name="IndexB">The 2nd vertex index.</param>
/// <param name="IndexC">The 3rd vertex index.</param>
public readonly record struct TriangleIndices(int IndexA, int IndexB, int IndexC) : IValidated
{
	public readonly bool IsValid()
	{
		bool isValid =
			IndexA >= 0 &&
			IndexB >= 0 &&
			IndexC >= 0 &&
			IndexA != IndexB &&
			IndexB != IndexC &&
			IndexA != IndexC;
		return isValid;
	}

	public readonly override string ToString() => $"[{IndexA}, {IndexB}, {IndexC}]";
}

/// <summary>
/// A structure containing 3 pieces of basic vertex data that make up a triangular polygon.
/// </summary>
public readonly record struct BasicTriangleData
{
	public readonly BasicVertex VertexA;
	public readonly BasicVertex VertexB;
	public readonly BasicVertex VertexC;

	/// <summary>
	/// Creates a new set of basic triangle data.
	/// </summary>
	/// <param name="_vertexA">The 1st vertex.</param>
	/// <param name="_vertexB">The 2nd vertex.</param>
	/// <param name="_vertexC">The 3rd vertex.</param>
	public BasicTriangleData(
		BasicVertex _vertexA,
		BasicVertex _vertexB,
		BasicVertex _vertexC)
	{
		VertexA = _vertexA;
		VertexB = _vertexB;
		VertexC = _vertexC;
	}

	/// <summary>
	/// Creates a new set of basic triangle data.
	/// </summary>
	/// <param name="_vertices">A list of vertex data, may not be null.</param>
	/// <param name="_triangleIndices">Indices for all three vertices of the triangle.</param>
	/// <exception cref="ArgumentNullException">Basic vertex data array may not be null.</exception>
	public BasicTriangleData(
		in IReadOnlyList<BasicVertex> _vertices,
		in TriangleIndices _triangleIndices)
	{
		ArgumentNullException.ThrowIfNull(_vertices);

		VertexA = _vertices[_triangleIndices.IndexA];
		VertexB = _vertices[_triangleIndices.IndexB];
		VertexC = _vertices[_triangleIndices.IndexC];
	}

	/// <summary>
	/// Gets the average/mean position of all 3 vertices.
	/// </summary>
	public readonly Vector3 AveragePosition => (VertexA.position + VertexB.position + VertexC.position) * 0.33333f;
	/// <summary>
	/// Gets the average/mean normal vector of all 3 vertices.
	/// </summary>
	public readonly Vector3 AverageNormal => (VertexA.normal + VertexB.normal + VertexC.normal) * 0.33333f;
}

/// <summary>
/// A structure containing 3 pieces of basic and extended vertex data that make up a triangular polygon.
/// </summary>
public readonly record struct ExtendedTriangleData
{
	public readonly BasicVertex VertexBasicA;
	public readonly BasicVertex VertexBasicB;
	public readonly BasicVertex VertexBasicC;
	public readonly ExtendedVertex VertexExtA;
	public readonly ExtendedVertex VertexExtB;
	public readonly ExtendedVertex VertexExtC;

	/// <summary>
	/// Creates a new set of triangle data.
	/// </summary>
	/// <param name="_vertexBasicA">The 1st vertex's basic data.</param>
	/// <param name="_vertexBasicB">The 2nd vertex's basic data.</param>
	/// <param name="_vertexBasicC">The 3rd vertex's basic data.</param>
	/// <param name="_vertexExtA">The 1st vertex's extended data.</param>
	/// <param name="_vertexExtB">The 2nd vertex's extended data.</param>
	/// <param name="_vertexExtC">The 3rd vertex's extended data.</param>
	public ExtendedTriangleData(
		BasicVertex _vertexBasicA,
		BasicVertex _vertexBasicB,
		BasicVertex _vertexBasicC,
		ExtendedVertex _vertexExtA,
		ExtendedVertex _vertexExtB,
		ExtendedVertex _vertexExtC)
	{
		VertexBasicA = _vertexBasicA;
		VertexBasicB = _vertexBasicB;
		VertexBasicC = _vertexBasicC;
		VertexExtA = _vertexExtA;
		VertexExtB = _vertexExtB;
		VertexExtC = _vertexExtC;
	}

	/// <summary>
	/// Creates a new set of triangle data.
	/// </summary>
	/// <param name="_verticesBasic">A list of basic vertex data, may not be null.</param>
	/// <param name="_verticesExt">A list of extended vertex data, may not be null.</param>
	/// <param name="_triangleIndices">Indices for all three vertices of the triangle.</param>
	/// <exception cref="ArgumentNullException">Basic and extended vertex data arrays may not be null.</exception>
	public ExtendedTriangleData(
		in IReadOnlyList<BasicVertex> _verticesBasic,
		in IReadOnlyList<ExtendedVertex> _verticesExt,
		TriangleIndices _triangleIndices)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);
		ArgumentNullException.ThrowIfNull(_verticesExt);

		VertexBasicA = _verticesBasic[_triangleIndices.IndexA];
		VertexBasicB = _verticesBasic[_triangleIndices.IndexB];
		VertexBasicC = _verticesBasic[_triangleIndices.IndexC];
		VertexExtA = _verticesExt[_triangleIndices.IndexA];
		VertexExtB = _verticesExt[_triangleIndices.IndexB];
		VertexExtC = _verticesExt[_triangleIndices.IndexC];
	}

	/// <summary>
	/// Gets only the basic vertex data of this triangle.
	/// </summary>
	public readonly BasicTriangleData BasicData => new(VertexBasicA, VertexBasicB, VertexBasicC);
}
