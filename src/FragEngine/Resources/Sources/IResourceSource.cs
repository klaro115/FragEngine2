using FragEngine.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Sources;

/// <summary>
/// Interface for different types of sources from where a resource may be loaded.
/// </summary>
public interface IResourceSource : IValidated, IExtendedDisposable
{
	#region Methods

	/// <summary>
	/// Performs a check if a specfic resource exists within this source.
	/// </summary>
	/// <param name="_sourceKey">A unique identifier key for locating a resource at the source.
	/// This is typically either a URI, or a stringified ID.</param>
	/// <param name="_sourceId">A unique ID number for locating a resource. May work in tandem with
	/// the '<see cref="_resourceKey"/>' parameter. This could be the index of a sub-resource, or
	/// a hash value that identifies the resource.</param>
	/// <returns>True if the resource exists within this source, false otherwise.</returns>
	bool CheckIfResourceExists(string? _sourceKey, int _sourceId);

	/// <summary>
	/// Tries to open a stream from which the resource data or a resource file may be loaded.
	/// </summary>
	/// <param name="_sourceKey">A unique identifier key for locating a resource at the source.
	/// This is typically either a URI, or a stringified ID.</param>
	/// <param name="_sourceId">A unique ID number for locating a resource. May work in tandem with
	/// the '<see cref="_resourceKey"/>' parameter. This could be the index of a sub-resource, or
	/// a hash value that identifies the resource.</param>
	/// <param name="_outStream">Outputs a stream from which the resource can be read. This stream
	/// will generally be read-only. Null if retrieving the resource fails.</param>
	/// <returns>True if the resource could be found and opened, false otherwise.</returns>
	bool OpenResourceStream(string? _sourceKey, int _sourceId, [NotNullWhen(true)] out Stream? _outStream);

	#endregion
}
