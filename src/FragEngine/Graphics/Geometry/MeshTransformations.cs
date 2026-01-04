using FragEngine.Scenes;
using System.Diagnostics;
using System.Numerics;

namespace FragEngine.Graphics.Geometry;

/// <summary>
/// Helper class for performing simple transformations on polygonal mesh geometry.
/// </summary>
/// <remarks>
/// Note: These transformations are all executed on the CPU and may not be very efficient. They are intended for
/// convenience when modifying geometry prior to its use in rendering, and for debugging purposes. If you just want
/// to rotate a model in your scene, you should use shaders instead and apply a <see cref="Pose"/> directly on the GPU.
/// </remarks>
public static class MeshTransformations
{
	#region Types

	/// <summary>
	/// A method that generates or modifies basic vertex data.
	/// </summary>
	/// <param name="_vertexBasic">The original vertex data.</param>
	/// <param name="_vertexIndex">The index of this vertex.</param>
	/// <returns>The modified vertex data.</returns>
	public delegate BasicVertex FuncModifyBasicVertex(in BasicVertex _vertexBasic, int _vertexIndex);

	/// <summary>
	/// A method that generates or modifies extended vertex data.
	/// </summary>
	/// <param name="_vertexExt">The original vertex data.</param>
	/// <param name="_vertexIndex">The index of this vertex.</param>
	/// <returns>The modified vertex data.</returns>
	public delegate ExtendedVertex FuncModifyExtendedVertex(in ExtendedVertex _vertexExt, int _vertexIndex);

	#endregion
	#region Methods

	/// <summary>
	/// Moves all vertex positions by a certain offset.
	/// </summary>
	/// <param name="_vertices">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to translate. May not exceed the length of <paramref name="_vertices"/>.</param>
	/// <param name="_offset">A position offset that is added to the position of each vertex.</param>
	/// <exception cref="ArgumentNullException">Vertex array may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Translate(BasicVertex[] _vertices, int _vertexCount, Vector3 _offset)
	{
		ArgumentNullException.ThrowIfNull(_vertices);

		if (_vertexCount > _vertices.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_vertices[i].position += _offset;
		}
	}

	/// <summary>
	/// Moves all vertex positions by a certain offset.c
	/// </summary>
	/// <param name="_meshData">Mesh data that shall be modified, may not be null or invalid.</param>
	/// <param name="_offset">A position offset that is added to the position of each vertex.</param>
	/// <exception cref="ArgumentNullException">Mesh data may not be null.</exception>
	public static void Translate(MeshSurfaceData _meshData, Vector3 _offset)
	{
		ArgumentNullException.ThrowIfNull(_meshData);
		Debug.Assert(_meshData.IsValid(), $"Cannot translate invalid {nameof(MeshSurfaceData)}!");

		Translate(_meshData.VerticesBasic, _meshData.VertexCount, _offset);
	}

	/// <summary>
	/// Scales all vertex position relative to the coordinate origin.
	/// </summary>
	/// <param name="_vertices">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to scale. May not exceed the length of <paramref name="_vertices"/>.</param>
	/// <param name="_scaleFactor">A scaling factor that each vertex position is multiplied with.</param>
	/// <exception cref="ArgumentNullException">Vertex array may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Scale(BasicVertex[] _vertices, int _vertexCount, float _scaleFactor)
	{
		ArgumentNullException.ThrowIfNull(_vertices);

		if (_vertexCount > _vertices.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_vertices[i].position *= _scaleFactor;
		}
	}

	/// <summary>
	/// Scales all vertex position relative to the coordinate origin.
	/// </summary>
	/// <param name="_vertices">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to scale. May not exceed the length of <paramref name="_vertices"/>.</param>
	/// <param name="_scaleFactors">Scaling factors that each vertex position is multiplied with component-wise.</param>
	/// <exception cref="ArgumentNullException">Vertex array may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Scale(BasicVertex[] _vertices, int _vertexCount, Vector3 _scaleFactors)
	{
		ArgumentNullException.ThrowIfNull(_vertices);

		if (_vertexCount > _vertices.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_vertices[i].position *= _scaleFactors;
		}
	}

	/// <summary>
	/// Scales all vertex position relative to the coordinate origin.
	/// </summary>
	/// <param name="_meshData">Mesh data that shall be modified, may not be null or invalid.</param>
	/// <param name="_scaleFactors">Scaling factors that each vertex position is multiplied with component-wise.</param>
	/// <exception cref="ArgumentNullException">Mesh data may not be null.</exception>
	public static void Scale(MeshSurfaceData _meshData, Vector3 _scaleFactors)
	{
		ArgumentNullException.ThrowIfNull(_meshData);
		Debug.Assert(_meshData.IsValid(), $"Cannot scale invalid {nameof(MeshSurfaceData)}!");

		Scale(_meshData.VerticesBasic, _meshData.VertexCount, _scaleFactors);
	}

	/// <summary>
	/// Applies a transformation to all vertices.
	/// </summary>
	/// <param name="_vertices">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to transform. May not exceed the length of <paramref name="_vertices"/>.</param>
	/// <param name="_mtxTransformation">A transformation matrix that is applied to each vertex' position and normals.</param>
	/// <param name="_normalizeVectors">Whether to normalize the length of all direction vectors after transformation.</param>
	/// <exception cref="ArgumentNullException">Vertex array may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Transform(BasicVertex[] _vertices, int _vertexCount, in Matrix4x4 _mtxTransformation, bool _normalizeVectors = true)
	{
		ArgumentNullException.ThrowIfNull(_vertices);

		if (_vertexCount > _vertices.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			Vector3 position = Vector3.Transform(_vertices[i].position, _mtxTransformation);
			Vector3 normal = Vector3.TransformNormal(_vertices[i].normal, _mtxTransformation);

			if (_normalizeVectors)
			{
				normal = Vector3.Normalize(normal);
			}

			_vertices[i].position = position;
			_vertices[i].normal = normal;
		}
	}

	/// <summary>
	/// Applies a transformation to all vertices.
	/// </summary>
	/// <param name="_verticesBasic">An array of basic vertices, may not be null.</param>
	/// <param name="_verticesExt">An array of extended vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to transform. May not exceed the length of <paramref name="_verticesBasic"/>.</param>
	/// <param name="_mtxTransformation">A transformation matrix that is applied to each vertex' position and normals.</param>
	/// <param name="_normalizeVectors">Whether to normalize the length of all direction vectors after transformation.</param>
	/// <exception cref="ArgumentNullException">Vertex arrays may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Transform(BasicVertex[] _verticesBasic, ExtendedVertex[] _verticesExt, int _vertexCount, in Matrix4x4 _mtxTransformation, bool _normalizeVectors = true)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);
		ArgumentNullException.ThrowIfNull(_verticesExt);

		if (_vertexCount > _verticesBasic.Length || _vertexCount > _verticesExt.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex arrays' length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			Vector3 position = Vector3.Transform(_verticesBasic[i].position, _mtxTransformation);
			Vector3 normal = Vector3.TransformNormal(_verticesBasic[i].normal, _mtxTransformation);
			Vector3 tangent = Vector3.TransformNormal(_verticesExt[i].tangent, _mtxTransformation);
			Vector3 binormal = Vector3.TransformNormal(_verticesExt[i].binormal, _mtxTransformation);

			if (_normalizeVectors)
			{
				normal = Vector3.Normalize(normal);
				tangent = Vector3.Normalize(tangent);
				binormal = Vector3.Normalize(binormal);
			}

			_verticesBasic[i].position = position;
			_verticesBasic[i].normal = normal;
			_verticesExt[i].tangent = tangent;
			_verticesExt[i].binormal = binormal;
		}
	}

	/// <summary>
	/// Applies a transformation to all vertices.
	/// </summary>
	/// <param name="_meshData">Mesh data that shall be modified, may not be null or invalid.</param>
	/// <param name="_mtxTransformation">A transformation matrix that is applied to each vertex' position and normals.</param>
	/// <param name="_normalizeVectors">Whether to normalize the length of all direction vectors after transformation.</param>
	/// <exception cref="ArgumentNullException">Mesh data may not be null.</exception>
	public static void Transform(MeshSurfaceData _meshData, in Matrix4x4 _mtxTransformation, bool _normalizeVectors = false)
	{
		ArgumentNullException.ThrowIfNull(_meshData);
		Debug.Assert(_meshData.IsValid(), $"Cannot transform invalid {nameof(MeshSurfaceData)}!");

		if (_meshData.HasExtendedVertexData)
		{
			Transform(_meshData.VerticesBasic, _meshData.VerticesExt!, _meshData.VertexCount, in _mtxTransformation, _normalizeVectors);
		}
		else
		{
			Transform(_meshData.VerticesBasic, _meshData.VertexCount, in _mtxTransformation, _normalizeVectors);
		}
	}

	/// <summary>
	/// Rotates all vertices around the coordinate origin.
	/// </summary>
	/// <param name="_vertices">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to rotate. May not exceed the length of <paramref name="_vertices"/>.</param>
	/// <param name="_rotation">A unit quaternion that describes a rotation that is applied to each vertex' position and normals.</param>
	/// <param name="_normalizeVectors">Whether to normalize the length of all direction vectors after transformation.</param>
	/// <exception cref="ArgumentNullException">Vertex array may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Rotate(BasicVertex[] _vertices, int _vertexCount, Quaternion _rotation, bool _normalizeVectors = true)
	{
		ArgumentNullException.ThrowIfNull(_vertices);

		if (_vertexCount > _vertices.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			Vector3 position = Vector3.Transform(_vertices[i].position, _rotation);
			Vector3 normal = Vector3.Transform(_vertices[i].normal, _rotation);

			if (_normalizeVectors)
			{
				normal = Vector3.Normalize(normal);
			}

			_vertices[i].position = position;
			_vertices[i].normal = normal;
		}
	}

	/// <summary>
	/// Rotates all vertices around the coordinate origin.
	/// </summary>
	/// <param name="_verticesBasic">An array of basic vertices, may not be null.</param>
	/// <param name="_verticesExt">An array of extended vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to rotate. May not exceed the length of vertex arrays.</param>
	/// <param name="_rotation">A unit quaternion that describes a rotation that is applied to each vertex' position and normals.</param>
	/// <param name="_normalizeVectors">Whether to normalize the length of all direction vectors after transformation.</param>
	/// <exception cref="ArgumentNullException">Vertex arrays may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex arrays' length.</exception>
	public static void Rotate(BasicVertex[] _verticesBasic, ExtendedVertex[] _verticesExt, int _vertexCount, Quaternion _rotation, bool _normalizeVectors = false)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);

		if (_vertexCount > _verticesBasic.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex arrays' length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			Vector3 position = Vector3.Transform(_verticesBasic[i].position, _rotation);
			Vector3 normal = Vector3.Transform(_verticesBasic[i].normal, _rotation);
			Vector3 tangent = Vector3.Transform(_verticesExt[i].tangent, _rotation);
			Vector3 binormal = Vector3.Transform(_verticesExt[i].binormal, _rotation);

			if (_normalizeVectors)
			{
				normal = Vector3.Normalize(normal);
				tangent = Vector3.Normalize(tangent);
				binormal = Vector3.Normalize(binormal);
			}

			_verticesBasic[i].position = position;
			_verticesBasic[i].normal = normal;
			_verticesExt[i].tangent = tangent;
			_verticesExt[i].binormal = binormal;
		}
	}

	/// <summary>
	/// Rotates all vertices around the coordinate origin.
	/// </summary>
	/// <param name="_meshData">Mesh data that shall be modified, may not be null or invalid.</param>
	/// <param name="_rotation">A unit quaternion that describes a rotation that is applied to each vertex' position and normals.</param>
	/// <param name="_normalizeVectors">Whether to normalize the length of all direction vectors after transformation.</param>
	/// <exception cref="ArgumentNullException">Mesh data may not be null.</exception>
	public static void Rotate(MeshSurfaceData _meshData, Quaternion _rotation, bool _normalizeVectors = false)
	{
		ArgumentNullException.ThrowIfNull(_meshData);
		Debug.Assert(_meshData.IsValid(), $"Cannot rotate invalid {nameof(MeshSurfaceData)}!");

		if (_meshData.HasExtendedVertexData)
		{
			Rotate(_meshData.VerticesBasic, _meshData.VerticesExt!, _meshData.VertexCount, _rotation, _normalizeVectors);
		}
		else
		{
			Rotate(_meshData.VerticesBasic, _meshData.VertexCount, _rotation, _normalizeVectors);
		}
	}

	/// <summary>
	/// Applies a user-defined modification to all vertices.
	/// </summary>
	/// <param name="_verticesBasic">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to modify. May not exceed the length of <paramref name="_verticesBasic"/>.</param>
	/// <param name="_action">An action that generates or modifies vertex data, may not be null.</param>
	/// <exception cref="ArgumentNullException">Vertex array and <paramref name="_action"/> delegate may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Foreach(BasicVertex[] _verticesBasic, int _vertexCount, FuncModifyBasicVertex _action)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);
		ArgumentNullException.ThrowIfNull(_action);

		if (_vertexCount > _verticesBasic.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_verticesBasic[i] = _action(_verticesBasic[i], i);
		}
	}

	/// <summary>
	/// Applies a user-defined modification to all vertices.
	/// </summary>
	/// <param name="_verticesExt">An array of extended vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to modify. May not exceed the length of <paramref name="_verticesExt"/>.</param>
	/// <param name="_action">An action that generates or modifies vertex data, may not be null.</param>
	/// <exception cref="ArgumentNullException">Vertex array and <paramref name="_action"/> delegate may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Vertex count may exceed vertex array's length.</exception>
	public static void Foreach(ExtendedVertex[] _verticesExt, int _vertexCount, FuncModifyExtendedVertex _action)
	{
		ArgumentNullException.ThrowIfNull(_verticesExt);
		ArgumentNullException.ThrowIfNull(_action);

		if (_vertexCount > _verticesExt.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_verticesExt[i] = _action(_verticesExt[i], i);
		}
	}

	/// <summary>
	/// Applies a user-defined modification to all vertices.
	/// </summary>
	/// <param name="_meshData">Mesh data that shall be modified, may not be null or invalid.</param>
	/// <param name="_actionBasic">An action that generates or modifies basic vertex data.</param>
	/// <param name="_actionExt">An action that generates or modifies extended vertex data.</param>
	/// <exception cref="ArgumentNullException">Mesh data and <paramref name="_action"/> delegate may not be null.</exception>
	public static void Foreach(MeshSurfaceData _meshData, FuncModifyBasicVertex? _actionBasic, FuncModifyExtendedVertex? _actionExt)
	{
		ArgumentNullException.ThrowIfNull(_meshData);
		Debug.Assert(_meshData.IsValid(), $"Cannot modify invalid {nameof(MeshSurfaceData)}!");

		if (_actionBasic is not null)
		{
			Foreach(_meshData.VerticesBasic, _meshData.VertexCount, _actionBasic);
		}
		if (_actionExt is not null && _meshData.HasExtendedVertexData)
		{
			Foreach(_meshData.VerticesExt!, _meshData.VertexCount, _actionExt);
		}
	}

	/// <summary>
	/// Normalizes length of all normal vectors.
	/// </summary>
	/// <param name="_verticesBasic">An array of basic vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to normalize. May not exceed the length of <paramref name="_verticesBasic"/>.</param>
	public static void NormalizeVectors(BasicVertex[] _verticesBasic, int _vertexCount)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);

		if (_vertexCount > _verticesBasic.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex array's length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_verticesBasic[i].normal = Vector3.Normalize(_verticesBasic[i].normal);
		}
	}

	/// <summary>
	/// Normalizes length of all normal vectors.
	/// </summary>
	/// <param name="_verticesBasic">An array of basic vertices, may not be null.</param>
	/// <param name="_verticesExt">An array of extended vertices, may not be null.</param>
	/// <param name="_vertexCount">The number of vertices to normalize. May not exceed the length of vertex arrays.</param>
	public static void NormalizeVectors(BasicVertex[] _verticesBasic, ExtendedVertex[] _verticesExt, int _vertexCount)
	{
		ArgumentNullException.ThrowIfNull(_verticesBasic);

		if (_vertexCount > _verticesBasic.Length || _vertexCount > _verticesExt.Length)
		{
			throw new IndexOutOfRangeException("Vertex count exceeded vertex arrays' length!");
		}

		for (int i = 0; i < _vertexCount; ++i)
		{
			_verticesBasic[i].normal = Vector3.Normalize(_verticesBasic[i].normal);
			_verticesExt[i].tangent = Vector3.Normalize(_verticesExt[i].tangent);
			_verticesExt[i].binormal = Vector3.Normalize(_verticesExt[i].binormal);
		}
	}

	/// <summary>
	/// Normalizes length of all normal vectors.
	/// </summary>
	/// <param name="_meshData">Mesh data that shall be normalized, may not be null or invalid.</param>
	/// <exception cref="ArgumentNullException">Mesh data may not be null.</exception>
	public static void NormalizeVectors(MeshSurfaceData _meshData)
	{
		ArgumentNullException.ThrowIfNull(_meshData);
		Debug.Assert(_meshData.IsValid(), $"Cannot modify invalid {nameof(MeshSurfaceData)}!");

		if (_meshData.HasExtendedVertexData)
		{
			NormalizeVectors(_meshData.VerticesBasic, _meshData.VerticesExt!, _meshData.VertexCount);
		}
		else
		{
			NormalizeVectors(_meshData.VerticesBasic, _meshData.VertexCount);
		}
	}

	#endregion
}
