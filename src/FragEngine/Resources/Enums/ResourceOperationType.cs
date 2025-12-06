namespace FragEngine.Resources.Enums;

/// <summary>
/// Enumeration of different resource file/data operations. Basically, whether to import or export.
/// </summary>
public enum ResourceOperationType
{
	/// <summary>
	/// Data is read from source, and converted into a usable resource.
	/// </summary>
	Import,
	/// <summary>
	/// A resource is serialized, and written out as a data.
	/// </summary>
	Export,
}
