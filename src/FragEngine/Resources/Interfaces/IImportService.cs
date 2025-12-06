using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Interfaces;

/// <summary>
/// Interface for engine services that implement import/export management for specific resource types.
/// Importers for supported resource types and sub-types may be registered with implementations of this
/// interface.
/// </summary>
public interface IImportService
{
	#region Properties

	/// <summary>
	/// Gets whether this import service hosts any resource importers.
	/// </summary>
	public bool HasImporters {  get; }
	/// <summary>
	/// Gets whether this import service hosts any resource exporters.
	/// </summary>
	public bool HasExporters {  get; }

	#endregion
	#region Methods

	/// <summary>
	/// Checks whether a combination of resource type and sub-type are supported by this import service.
	/// </summary>
	/// <remarks>
	/// This method is used to identify the importer with the highest level of support for any given
	/// combination of resource type and sub-type. The first fully compatible is chosen from all available
	/// importer services. If no importer explicitly caters to the exact sub-type, the first importer with
	/// full support for the default sub-type (i.e. value=<c>0</c>) is used instead.
	/// </remarks>
	/// <param name="_type">The type of resource.</param>
	/// <param name="_subType">The sub-type index for a specific variation of a resource type. Default is zero.
	/// If no specific importer for a sub-type exists, the </param>
	/// <returns>An enum value describing the level of support for this resource type and sub-type:
	/// <list type="bullet">
	///		<item>Returns '<see cref="ResourceTypeSupport.SubTypeSupported"/>' if the importer offers explicit
	///		support for the given resource sub-type.</item>
	///		<item>Returns '<see cref="ResourceTypeSupport.BaseTypeSupported"/>' if there is support for the
	///		resource type, but not necessarily for this specific sub-type.</item>
	///		<item>Returns '<see cref="ResourceTypeSupport.None"/>' if either the resource type is not supported
	///		at all, or if this specific sub-type is explicilty unsupported.</item>
	/// </list>
	/// </returns>
	ResourceTypeSupport IsResourceTypeSupported(ResourceType _type, int _subType = 0);

	/// <summary>
	/// Checks whether a specific format key is supported for import or export.
	/// </summary>
	/// <param name="_formatKey">A format identifier string, may not be null. This is typically the file
	/// extension of the resource when exported to file.<para/>
	/// NOTE: File format extensions must include the leading period character and are assumed to be all lowercase.
	/// See '<see cref="ResourceData.FormatKey"/>' for details.</param>
	/// <param name="_operation">For which operation to check support, i.e. import or export.</param>
	/// <returns>True if resource data with the given format key is supported for the given operation, false
	/// otherwise.</returns>
	bool IsResourceFormatKeySupported(string _formatKey, ResourceOperationType _operation);

	/// <summary>
	/// Try to import a resource.
	/// </summary>
	/// <param name="_resourceData">Resource data describing what to import and from where. May not be null.</param>
	/// <param name="_outResourceInstance">Outputs the fully loaded resource instance, or null, if import failed.</param>
	/// <returns>True if the resource instance was imported successfully, false if import failed or if the operation
	/// was not supported.</returns>
	/// <exception cref="ArgumentNullException">Resource data may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Importer and its dependencies may not be disposed.</exception>
	bool ImportResourceData(in ResourceData _resourceData, [NotNullWhen(true)] out object? _outResourceInstance);

	//... (add export support later)

	#endregion
}
