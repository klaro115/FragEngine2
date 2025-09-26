using FragEngine.Extensions;
using FragEngine.Interfaces;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// A container for geometry data for a polygonal mesh surface.
/// </summary>
public sealed class MeshSurfaceData : IValidated
{
	#region Fields

	/// <summary>
	/// An array of basic vertex data.
	/// Each element defines a position, a normal vector, and texture coordinates.
	/// </summary>
	public BasicVertex[] VerticesBasic { get; private set; } = [];
	/// <summary>
	/// Optional. An array of additional vertex data.
	/// This must have at least as many elements as <see cref="VerticesBasic"/>.
	/// </summary>
	public ExtendedVertex[]? VerticesExt { get; private set; } = null;

	/// <summary>
	/// An array of triangle indices. Each element is a 16-bit index to a vertex.
	/// Each group of 3 indices describe a triangular polygon.
	/// </summary>
	/// <remarks>
	/// Note: 16-bit indices allow for a maximum of 65K vertices. If the mesh has
	/// more vertices, this array will be null, and the '<see cref="Indices32"/>'
	/// array will be used instead.
	/// </remarks>
	public ushort[]? Indices16 { get; private set; } = null;
	/// <summary>
	/// An array of triangle indices. Each element is a 32-bit index to a vertex.
	/// Each group of 3 indices describe a triangular polygon.
	/// </summary>
	/// <remarks>
	/// Note: 32-bit indices allow for a maximum of 4 billion vertices, but require
	/// twice as much memory per element than 16-bit indices. If the mesh has fewer
	/// than 65K vertices, this array will be null, and the '<see cref="Indices16"/>'
	/// array will be used instead.
	/// </remarks>
	public int[]? Indices32 { get; private set; } = null;

	/// <summary>
	/// Gets the number of vertices that have at least basic vertex data.
	/// </summary>
	public int VertexCount => VerticesBasic.Length;
	/// <summary>
	/// Gets the number of triangle indices.
	/// </summary>
	public int IndexCount { get; private set; } = 0;
	/// <summary>
	/// Gets the number of triangular polygons.
	/// </summary>
	public int TriangleCount { get; private set; } = 0;

	/// <summary>
	/// Gets whether this mesh has a full set of extended vertex data.
	/// </summary>
	public bool HasExtendedVertexData => VerticesExt is not null && VerticesExt.Length >= VertexCount;
	/// <summary>
	/// Gets the format of indices, either 16-bit or 32-bit.
	/// </summary>
	///	<remarks>
	///	Note: 16-bit integers take up half as much memory as 32-bit integers,
	///	but they also limit the mesh to have no more than 65K vertices. If
	///	a mesh is very large and complex, it may not be possible to use 16-bit
	///	indices.
	///	</remarks>
	public IndexFormat IndexFormat { get; private set; } = IndexFormat.UInt16;

	#endregion
	#region Methods

	public bool IsValid()
	{
		bool isValid =
			VerticesBasic is not null &&
			(Indices16 is not null || Indices32 is not null) &&
			VertexCount > 0 &&
			IndexCount >= 3 &&
			TriangleCount > 0;
		return isValid;
	}

	/// <summary>
	/// Assigns vertex data to this mesh.
	/// </summary>
	/// <param name="_verticesBasic">The new basic vertex data, may not be null.</param>
	/// <param name="_verticesExt">Optional. The new extended vertex data. If null, the mesh will only have basic geometry data.</param>
	/// <param name="_vertexCount">The number of vertices to assign. If negative, the length of '<see cref="_verticesBasic"/>' is used instead.</param>
	/// <param name="_logger">Optional. A logging service, for outputting error messages. If null, no errors are logged.</param>
	/// <returns>True if vertex data could be assigned successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Basic vertex data array may not be null.</exception>
	public bool SetVertices(in IReadOnlyList<BasicVertex> _verticesBasic, in IReadOnlyList<ExtendedVertex>? _verticesExt, int _vertexCount = -1, ILogger? _logger = null)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);

		// Check completeness of data arrays:
		if (_vertexCount < 0)
		{
			_vertexCount = _verticesBasic.Count;
		}
		else if (_verticesBasic.Count < _vertexCount)
		{
			_logger?.LogError($"Array '{nameof(_verticesBasic)}' contains fewer elements than '{nameof(_vertexCount)}'!");
			return false;
		}
		if (_verticesExt is not null && _verticesExt.Count < _vertexCount)
		{
			_logger?.LogError($"Array '{nameof(_verticesExt)}' contains insufficient elements!");
			return false;
		}

		// Copy data to local arrays:
		if (_verticesBasic != VerticesBasic)
		{
			VerticesBasic = new BasicVertex[_vertexCount];
			_verticesBasic.CopyTo(VerticesBasic, 0, _vertexCount);
		}

		if (_verticesExt is not null && _verticesExt != VerticesExt)
		{
			VerticesExt = new ExtendedVertex[_vertexCount];
			_verticesExt.CopyTo(VerticesExt, 0, _vertexCount);
		}
		else if (_verticesExt is null)
		{
			VerticesExt = null;
		}

		return true;
	}

	/// <summary>
	/// Assigns index data to this mesh.
	/// </summary>
	/// <param name="_newIndices">The new index data in 16-bit format, may not be null.
	/// Each set of 3 indices define 1 triangular polygon, therefore count should be a multiple of 3.</param>
	/// <param name="_indexCount">The number of indices to assign. If negative, the length of <see cref="_newIndices"/> is used instead.</param>
	/// <param name="_indexFormat">The desired data format when assigning the indices. Default is 16-bit.</param>
	/// <param name="_logger">Optional. A logging service, for outputting error messages. If null, no errors are logged.</param>
	/// <returns>True if index data could be assigned successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Index array may not be null.</exception>
	public bool SetIndices16(in IReadOnlyList<ushort> _newIndices, int _indexCount = -1, IndexFormat _indexFormat = IndexFormat.UInt16, ILogger? _logger = null)
	{
		ArgumentNullException.ThrowIfNull(_newIndices);

		// Check completeness of data arrays:
		if (_indexCount < 0)
		{
			_indexCount = _newIndices.Count;
		}
		else if (_newIndices.Count < _indexCount)
		{
			_logger?.LogError($"Array '{nameof(_newIndices)}' contains fewer elements than '{nameof(_indexCount)}'!");
			return false;
		}

		// Copy data to local array:
		if (_indexFormat == IndexFormat.UInt32)
		{
			Indices16 = null;
			Indices32 = new int[_indexCount];
			for (int i = 0; i < _indexCount; i++)
			{
				Indices32[i] = _newIndices[i];
			}

		}
		else if (_newIndices != Indices16)
		{
			Indices16 = new ushort[_indexCount];
			Indices32 = null;
			_newIndices.CopyTo(Indices16, 0, _indexCount);
		}

		IndexCount = _indexCount;
		TriangleCount = IndexCount / 3;

		return true;
	}

	/// <summary>
	/// Assigns index data to this mesh.
	/// </summary>
	/// <param name="_newIndices">The new index data in 32-bit format, may not be null.
	/// Each set of 3 indices define 1 triangular polygon, therefore count should be a multiple of 3.</param>
	/// <param name="_indexCount">The number of indices to assign. If negative, the length of <see cref="_newIndices"/> is used instead.</param>
	/// <param name="_indexFormat">The desired data format when assigning the indices. Default is 32-bit.
	/// Do not set this value to 16-bit if the mesh has more than 65K vertices.</param>
	/// <param name="_logger">Optional. A logging service, for outputting error messages. If null, no errors are logged.</param>
	/// <returns>True if index data could be assigned successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Index array may not be null.</exception>
	public bool SetIndices32(in IReadOnlyList<int> _newIndices, int _indexCount = -1, IndexFormat _indexFormat = IndexFormat.UInt32, ILogger? _logger = null)
	{
		ArgumentNullException.ThrowIfNull(_newIndices);

		// Check completeness of data arrays:
		if (_indexCount < 0)
		{
			_indexCount = _newIndices.Count;
		}
		else if (_newIndices.Count < _indexCount)
		{
			_logger?.LogError($"Array '{nameof(_newIndices)}' contains fewer elements than '{nameof(_indexCount)}'!");
			return false;
		}

		// Copy data to local array:
		if (_indexFormat == IndexFormat.UInt16)
		{
			Indices16 = new ushort[_indexCount];
			Indices32 = null;
			for (int i = 0; i < _indexCount; i++)
			{
				Indices16[i] = (ushort)_newIndices[i];
			}

		}
		else if (_newIndices != Indices32)
		{
			Indices16 = null;
			Indices32 = new int[_indexCount];
			_newIndices.CopyTo(Indices32, 0, _indexCount);
		}

		IndexCount = _indexCount;
		TriangleCount = IndexCount / 3;

		return true;
	}

	/// <summary>
	/// Gets an enumeration of all triangles indices.
	/// </summary>
	public IEnumerable<TriangleIndices> EnumerateTriangleIndices()
	{
		if (IndexFormat == IndexFormat.UInt16)
		{
			for (int i = 0; i < TriangleCount; i++)
			{
				int j = 3 * i;
				yield return new TriangleIndices(
					Indices16![j + 0],
					Indices16[j + 1],
					Indices16[j + 2]);
			}
		}
		else
		{
			for (int i = 0; i < TriangleCount; i++)
			{
				int j = 3 * i;
				yield return new TriangleIndices(
					Indices32![j + 0],
					Indices32[j + 1],
					Indices32[j + 2]);
			}
		}
	}

	/// <summary>
	/// Gets an enumeration of the basic vertex data of all triangles.
	/// </summary>
	public IEnumerable<BasicTriangleData> EnumerateBasicTriangleData()
	{
		foreach (TriangleIndices indices in EnumerateTriangleIndices())
		{
			yield return new(VerticesBasic, indices);
		}
	}

	/// <summary>
	/// Gets an enumeration of the full (basic+extended) vertex data of all triangles.
	/// </summary>
	public IEnumerable<ExtendedTriangleData> EnumerateExtendedTriangleData()
	{
		if (!HasExtendedVertexData)
		{
			yield break;
		}

		foreach (TriangleIndices indices in EnumerateTriangleIndices())
		{
			yield return new(VerticesBasic, VerticesExt!, indices);
		}
	}

	#endregion
}
