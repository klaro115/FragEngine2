using FragEngine.Resources.Interfaces;

namespace FragEngine.Resources.Enums;

/// <summary>
/// Enumeration of different levels of support/compatibility between resource types and their sub-type,
/// and a given <see cref="IImportService"/>.
/// </summary>
public enum ResourceTypeSupport
{
	/// <summary>
	/// There is no support for this resource type.
	/// </summary>
	None,
	/// <summary>
	/// Full support for this exact sub-type of this resource type.
	/// </summary>
	SubTypeSupported,
	/// <summary>
	/// The resource type is supported, but there is no explicit support for this specific sub-type.
	/// </summary>
	BaseTypeSupported,
}
