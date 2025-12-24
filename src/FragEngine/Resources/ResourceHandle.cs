using FragEngine.Interfaces;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources;

/// <summary>
/// Base class for resource handles. This handle wraps identifiers to a resource/asset, as well as its loading state and allows access to the resource instance, once loaded.
/// </summary>
/// <param name="_resourceService">The engine's resource service, which is used for loading the resource's data.</param>
public abstract class ResourceHandle(ResourceService _resourceService) : IValidated, IExtendedDisposable
{
	#region Fields

	protected readonly ResourceService resourceService = _resourceService ?? throw new ArgumentNullException(nameof(_resourceService));

	#endregion
	#region Properties

	// STATE:

	public bool IsDisposed { get; protected set; } = false;

	/// <summary>
	/// Gets the current loading state of this resource.
	/// </summary>
	public ResourceLoadingState LoadingState { get; protected set; } = ResourceLoadingState.NotLoaded;

	/// <summary>
	/// Gets whether the resource is currently fully loaded and ready for use.
	/// </summary>
	public bool IsLoaded => !IsDisposed && LoadingState == ResourceLoadingState.Loaded;

	// IDENTIFIERS:

	/// <summary>
	/// Gets a unique identifier string for this resource.
	/// </summary>
	public required string ResourceKey { get; init; }
	/// <summary>
	/// Gets a unique identifier number for this resource, typically a hash of '<see cref="ResourceKey"/>'.
	/// </summary>
	public required int ResourceID { get; init; }

	// DATA:

	/// <summary>
	/// Gets the resource object, if it has been loaded already. Null if the resource is not loaded.
	/// </summary>
	public abstract object? ResourceObject { get; }

	#endregion
	#region Constructors

	~ResourceHandle()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	protected abstract void Dispose(bool _disposing);

	public abstract bool IsValid();

	/// <summary>
	/// Loads the resource if it isn't loaded yet.
	/// </summary>
	/// <param name="_loadImmediately">Whether to load the resource immediately on the calling thread.
	/// If false, the resource will be queued up for asynchronous loading in the background instead.</param>
	/// <returns>True if the resource is loaded or was successfully queued up for loading, false otherwise.</returns>
	public abstract bool Load(bool _loadImmediately);

	/// <summary>
	/// Loads the resource if it isn't loaded yet, and waits for background loading to finish.
	/// </summary>
	/// <returns>True if the resource was loaded successfully, false otherwise.</returns>
	public abstract Task<bool> LoadAsync();

	/// <summary>
	/// Unloads the resource, or abort any ongoing loading process.
	/// </summary>
	public abstract void Unload();

	/// <summary>
	/// Tries to get this resource's import data, if available.
	/// </summary>
	/// <param name="_outData">Outputs the import data matching this handle's '<see cref="ResourceHandle.ResourceKey"/>',
	/// or null, if no resource could be found.</param>
	/// <returns>True if the data was found, false otherwise or if the resource handle is invalid.</returns>
	public bool GetResourceData([NotNullWhen(true)] out ResourceData? _outData)
	{
		if (IsDisposed || LoadingState == ResourceLoadingState.FailedToLoad)
		{
			_outData = null;
			return false;
		}

		return resourceService.GetResourceData(ResourceKey, out _outData);
	}

	public static int CreateResourceID(string _resourceKey, ResourceType _type)
	{
		if (string.IsNullOrEmpty(_resourceKey) || _type == ResourceType.Unknown)
		{
			return -1;
		}

		string idBase = $"{_type}_{_resourceKey}";

		//TODO: Generate hash.
		int id = idBase.GetHashCode();	//TEMP

		return id;
	}

	#endregion
}

/// <summary>
/// A handle for resources of a specific type. This handle wraps identifiers to a resource/asset, as well as its loading state and allows access to the resource instance, once loaded.
/// </summary>
/// <typeparam name="T">The type of the resource instance.</typeparam>
/// <param name="_resourceService">The engine's resource service, which is used for loading the resource's data.</param>
public sealed class ResourceHandle<T>(ResourceService _resourceService) : ResourceHandle(_resourceService) where T : class
{
	#region Properties

	// DATA:

	public override object? ResourceObject => Resource;

	/// <summary>
	/// Gets the typed resource object, if it has been loaded already. Null if the resource is not loaded.
	/// </summary>
	public T? Resource { get; private set; } = default;

	#endregion
	#region Methods

	protected override void Dispose(bool _)
	{
		IsDisposed = true;

		DisposeResource();
	}

	private void DisposeResource()
	{
		if (Resource is IDisposable disposable)
		{
			disposable.Dispose();
		}
		Resource = null;
	}

	public override bool IsValid()
	{
		bool isValid =
			!IsDisposed &&
			!string.IsNullOrWhiteSpace(ResourceKey) &&
			!(IsLoaded && Resource is null) &&
			LoadingState != ResourceLoadingState.FailedToLoad;
		return isValid;
	}
	
	private void OnLoadingStateUpdated(ResourceLoadingState _newState)
	{
		LoadingState = IsDisposed
			? ResourceLoadingState.NotLoaded
			: _newState;
	}

	private void OnResourceLoaded(object _loadedResource)
	{
		ArgumentNullException.ThrowIfNull(_loadedResource);

		Debug.Assert(_loadedResource is not IExtendedDisposable disposable || !disposable.IsDisposed, "Newly loaded resource may not be disposed!");
		Debug.Assert(_loadedResource is T, "Newly loaded resource is not assignable to handle's resource type!");

		Resource = (_loadedResource as T)!;
		LoadingState = ResourceLoadingState.Loaded;

		if (IsDisposed)
		{
			DisposeResource();
		}
	}

	/// <summary>
	/// Gets the loaded resources, or tries to load it immediately if it isn't loaded yet.
	/// </summary>
	/// <param name="_outResource">Outputs the loaded resource, or null, if loading failed.</param>
	/// <returns>True if the resource was loaded, false otherwise.</returns>
	public bool GetOrLoadImmediately([NotNullWhen(true)] out T? _outResource)
	{
		if (!IsLoaded && !Load(true))
		{
			_outResource = null;
			return false;
		}

		_outResource = Resource!;
		return true;
	}

	public override bool Load(bool _loadImmediately)
	{
		if (IsLoaded)
		{
			return true;
		}
		// Abort if loading has failed before:
		if (IsDisposed || LoadingState == ResourceLoadingState.FailedToLoad)
		{
			return false;
		}
		// If the resource is already queued or loading in the background, all is well:
		if (LoadingState != ResourceLoadingState.NotLoaded && !_loadImmediately)
		{
			return true;
		}

		return resourceService.LoadResource(this, _loadImmediately, OnLoadingStateUpdated, OnResourceLoaded);
	}

	public override async Task<bool> LoadAsync()
	{
		if (IsLoaded)
		{
			return true;
		}
		// Abort if loading has failed before:
		if (IsDisposed || LoadingState == ResourceLoadingState.FailedToLoad)
		{
			return false;
		}

		return await resourceService.LoadResourceAsync(this, OnResourceLoaded);
	}

	public override void Unload()
	{
		if (IsLoaded)
		{
			if (Resource is IDisposable disposable)
			{
				disposable.Dispose();
			}
			Resource = null;
			return;
		}

		if (LoadingState is ResourceLoadingState.NotLoaded or ResourceLoadingState.FailedToLoad)
		{
			return;
		}

		resourceService.AbortLoading(this);
	}

	#endregion
	#region Methods Misc

	public static bool operator ==(ResourceHandle<T> _handle, T _resource) => _handle.Resource == _resource;
	public static bool operator !=(ResourceHandle<T> _handle, T _resource) => _handle.Resource != _resource;

	public override bool Equals(object? obj)
	{
		if (obj is null)
		{
			return false;
		}
		if (ReferenceEquals(this, obj))
		{
			return true;
		}
		if (obj is ResourceHandle<T> resourceHandle)
		{
			return ResourceKey == resourceHandle.ResourceKey;
		}
		if (obj is T resourceInstance)
		{
			return resourceInstance.Equals(Resource);
		}

		return false;
	}

	public override int GetHashCode() => base.GetHashCode();

	public override string ToString() => $"ResourceHandle (Key: '{ResourceKey}', ID: '{ResourceID}', State: '{LoadingState}', Resource: '{Resource?.ToString() ?? "NULL"}')";

	#endregion
}
