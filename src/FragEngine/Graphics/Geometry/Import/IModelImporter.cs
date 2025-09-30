using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Graphics.Geometry.Import;

/// <summary>
/// Interface for importer types that can load a 3D model from resources.
/// </summary>
public interface IModelImporter
{
	#region Methods

	/// <summary>
	/// Tries to load surface geometry data of a 3D model from a resource.
	/// </summary>
	/// <param name="_resourceStream">A resource stream from which the model data may be loaded. This stream is
	/// assumed to be read-only.</param>
	/// <param name="_outSurfaceData">Outputs the fully loaded mesh surface data, or null, if the import failed.</param>
	/// <returns>True if the data was loaded successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource stream may not be null.</exception>
	/// <exception cref="ArgumentException">Resource stream must support reading.</exception>
	bool LoadMeshSurfaceData(Stream _resourceStream, [NotNullWhen(true)] out MeshSurfaceData? _outSurfaceData);

	#endregion
}
