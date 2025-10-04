namespace FragEngine.Graphics.Geometry.Export;

/// <summary>
/// Interface for exporter types that can write a 3D model to resources.
/// </summary>
public interface IModelExporter
{
	#region Methods

	/// <summary>
	/// Tries to write surface geometry data of a 3D model to a resource.
	/// </summary>
	/// <param name="_resourceStream">A resource stream to which the model data may be saved. This stream is
	/// assumed to be write-only. This will usually be a file stream.</param>
	/// <param name="_surfaceData">The mesh's surface data that shall be written out as a resource.</param>
	/// <returns>True if the data was loaded successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource stream and surface data may not be null.</exception>
	/// <exception cref="ArgumentException">Resource stream must support writing.</exception>
	bool WriteMeshSurfaceData(Stream _resourceStream, in MeshSurfaceData _surfaceData);

	#endregion
}
