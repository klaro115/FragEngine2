namespace FragEngine.Resources;

/// <summary>
/// Delegate for callback methods that assign a loaded resource to the <see cref="ResourceHandle{T}"/> that initiated the loading.
/// </summary>
/// <param name="_loadedResource">The newly loaded resource instance.</param>
internal delegate void FuncAssignLoadedResource(object _loadedResource);
