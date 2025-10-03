using FragEngine.Interfaces;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources;

public abstract class ResourceHandle(ResourceService _resourceService) : IValidated, IExtendedDisposable
{
	#region Fields

	protected readonly ResourceService resourceService = _resourceService ?? throw new ArgumentNullException(nameof(_resourceService));

	#endregion
	#region Properties

	// STATE:

	public bool IsDisposed { get; protected set; } = false;

	public ResourceLoadingState LoadingState { get; protected set; } = ResourceLoadingState.NotLoaded;
	public bool IsLoaded => !IsDisposed && LoadingState == ResourceLoadingState.Loaded;

	// IDENTIFIERS:

	public required string ResourceKey { get; init; }
	public required int ResourceID { get; init; }

	// DATA:

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

	#endregion
}

public sealed class ResourceHandle<T>(ResourceService _resourceService) : ResourceHandle(_resourceService) where T : class
{
	#region Properties

	// DATA:

	public override object? ResourceObject => Resource;

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
	/// <returns>Treu if the resource was loaded, false otherwise.</returns>
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

	/// <summary>
	/// Loads the resource if it isn't loaded yet.
	/// </summary>
	/// <param name="_loadImmediately">Whether to load the resource immediately on the calling thread.
	/// If false, the resource will be queued up for asynchronous loading in the background instead.</param>
	/// <returns>True if the resource is loaded or was successfully queued up for loading, false otherwise.</returns>
	public bool Load(bool _loadImmediately)
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

		return resourceService.LoadResource(this, _loadImmediately, OnResourceLoaded);
	}

	/// <summary>
	/// Loads the resource if it isn't loaded yet, and waits for background loading to finish.
	/// </summary>
	/// <returns>True if the resource was loaded successfully, false otherwise.</returns>
	public async Task<bool> LoadAsync()
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

	/// <summary>
	/// Unloads the resource, or abort any ongoing loading process.
	/// </summary>
	public void Unload()
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
