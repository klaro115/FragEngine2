using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources;

/// <summary>
/// Delegate for callback methods that assign a loaded resource to the <see cref="ResourceHandle{T}"/> that initiated the loading.
/// </summary>
/// <param name="_loadedResource">The newly loaded resource instance.</param>
internal delegate void FuncAssignLoadedResource(object _loadedResource);

/// <summary>
/// Delegate for factory methods that create resource handles for a specific resource type.
/// </summary>
/// <remarks>
/// Note: This delegate is used by the <see cref="ResourceHandleFactory"/> service. Methods with this signature may be registered with the factory,
/// which will then use the method to create instances of <see cref="ResourceHandle{T}"/> with type and contents corresponding to the resource's type
/// and data.
/// </remarks>
/// <param name="_data">The resource data as declared in <see cref="ResourceManifest"/>. May not be null.</param>
/// <param name="_outHandle">Outputs a new handle corresponding to the <see cref="ResourceType"/> and generic data type of the given resource data.
/// Null, if the data was incomplete or invalid.</param>
/// <returns>True if a resource handle was successfully created for the given resource data, false otherwise.</returns>
public delegate bool FuncCreateResourceHandle(ResourceData _data, [NotNullWhen(true)] out ResourceHandle? _outHandle);
