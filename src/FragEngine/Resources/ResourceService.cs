using FragEngine.Graphics;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using FragEngine.Resources.Interfaces;
using FragEngine.Resources.Internal;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources;

public sealed class ResourceService : IExtendedDisposable
{
	#region Events

	/// <summary>
	/// Event that is triggered whenever one or more new resources have been registered with this service.
	/// </summary>
	public event Action? NewResourcesAdded = null;

	#endregion
	#region Fields

	private readonly ILogger logger;
	private readonly ResourceDataService resourceDataService;
	private readonly ResourceHandleFactory handleFactory;
	private readonly HashSet<IImportService> importServices = [];

	private readonly ResourceLoadQueue queue;
	private readonly Thread backgroundLoadThread;
	private readonly CancellationTokenSource loadCancellationTokenSrc = new();

	private readonly ConcurrentDictionary<string, ResourceHandle> allResources = new(-1, ResourceConstants.allResourcesStartingCapacity);

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	public ResourceService(ILogger _logger, ResourceDataService _resourceDataService, ResourceHandleFactory _handleFactory, GraphicsImportService _graphicsImportService)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_resourceDataService);
		ArgumentNullException.ThrowIfNull(_handleFactory);
		ArgumentNullException.ThrowIfNull(_graphicsImportService);

		logger = _logger;
		resourceDataService = _resourceDataService;
		handleFactory = _handleFactory;

		logger.LogStatus("# Initializing resource service.");

		RegisterImportService(_graphicsImportService);

		resourceDataService.ResourceDataScanCompleted += OnResourceDataScanCompleted;

		queue = new(logger);

		try
		{
			backgroundLoadThread = new(RunBackgroundLoading);
			backgroundLoadThread.Start();
		}
		catch (Exception ex)
		{
			Dispose();
			throw new Exception("Failed to start background resource loading thread!", ex);
		}
	}

	~ResourceService()
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

	private void Dispose(bool _)
	{
		IsDisposed = true;

		AbortBackgroundLoadingThread();

		queue.Dispose();
		loadCancellationTokenSrc.Dispose();
	}

	private void AbortBackgroundLoadingThread()
	{
		// Notify thread of the cancellation:
		loadCancellationTokenSrc.Cancel();

		// Wait for a bit for the thread to exit safely:
		const int waitIntervalMs = 4;
		int timeout = ResourceConstants.waitForLoadingToCompleteTimeoutMs;
		
		while (timeout > 0 && backgroundLoadThread.IsAlive)
		{
			Thread.Sleep(waitIntervalMs);
			timeout -= waitIntervalMs;
		}
	}

	private void RunBackgroundLoading()
	{
		if (IsDisposed)
		{
			return;
		}

		logger.LogStatus($"{nameof(ResourceService)}: Background loading thread started.");

		string lastResourceKey = string.Empty;

		try
		{
			while (!IsDisposed && !loadCancellationTokenSrc.IsCancellationRequested)
			{
				// If the queue is empty, sleep the thread for a while:
				if (!queue.Dequeue(out ResourceLoadHandle? loadingHandle))
				{
					Thread.Sleep(10);
					continue;
				}

				lastResourceKey = loadingHandle.ResourceHandle.ResourceKey;

				// Import resource immediately on this background thread:
				bool result = LoadImmediately(loadingHandle.ResourceHandle, loadingHandle.AssignResourceCallback);

				loadingHandle.SetLoadingResult(result);
			}
		}
		catch (Exception ex)
		{
			logger.LogException("An unhandled exception was caught in the background resource loading thread!", ex, LogEntrySeverity.Fatal);
			logger.LogError($"Exiting background loading loop. Last loaded resource: '{lastResourceKey}'");
		}
		finally
		{
			logger.LogStatus($"{nameof(ResourceService)}: Background loading thread exited.");
		}
	}

	private void OnResourceDataScanCompleted()
	{
		if (IsDisposed) return;

		// Identify any resource keys that are new:
		if (!resourceDataService.GetAllResourceData(out IReadOnlyDictionary<string, ResourceData>? allResourceData))
		{
			logger.LogError("Failed to update resource handles from latest data scan result!");
			return;
		}

		int expectedNewResourceCount = Math.Max(allResourceData.Count - allResources.Count, 1);
		Dictionary<string, ResourceHandle> newHandles = new(expectedNewResourceCount);

		foreach ((string resourceKey, ResourceData data) in allResourceData)
		{
			if (allResources.ContainsKey(resourceKey))
			{
				continue;
			}

			// Create new hard-typed resource handle via factory:
			if (!handleFactory.TryCreateResourceHandle(data, out ResourceHandle? handle))
			{
				continue;
			}

			newHandles.Add(resourceKey, handle);
		}

		// No new keys found? Exit here:
		if (newHandles.Count == 0)
		{
			return;
		}

		// Register all newly detected handles:
		int addedHandleCount = 0;
		foreach ((string resourceKey, ResourceHandle newHandle) in newHandles)
		{
			if (allResources.TryAdd(resourceKey, newHandle))
			{
				addedHandleCount++;
			}
		}

		logger.LogMessage($"Resource data scan detected {addedHandleCount} new resource keys.");
		NewResourcesAdded?.Invoke();
	}

	/// <summary>
	/// Checks if a resource exists.
	/// </summary>
	/// <param name="_resourceKey">A unique identifier key for the resource.</param>
	/// <returns>True if a resource with this key exists, false otherwise.</returns>
	public bool HasResource(string _resourceKey)
	{
		if (IsDisposed)
		{
			logger.LogError("Cannot check resources of disposed resource service!");
			return false;
		}
		if (string.IsNullOrEmpty(_resourceKey))
		{
			logger.LogError("Null or blank resource keys are not permitted!");
			return false;
		}

		return allResources.ContainsKey(_resourceKey);
	}

	/// <summary>
	/// Gets a handle for a resource.
	/// </summary>
	/// <param name="_resourceKey">A unique identifier key for the resource.</param>
	/// <param name="_outHandle">Outputs a handle for the resource, or null, if the resource could not be found.</param>
	/// <returns>True if a resource handle was found, false otherwise.</returns>
	public bool GetResourceHandle(string _resourceKey, [NotNullWhen(true)] out ResourceHandle? _outHandle)
	{
		if (IsDisposed)
		{
			logger.LogError("Cannot get resource handle from disposed resource service!");
			_outHandle = null;
			return false;
		}
		if (string.IsNullOrEmpty(_resourceKey))
		{
			logger.LogError("Cannot get resource handle for null or blank resource key!");
			_outHandle = null;
			return false;
		}

		return allResources.TryGetValue(_resourceKey, out _outHandle);
	}

	/// <summary>
	/// Gets a typed handle for a resource.
	/// </summary>
	/// <typeparam name="T">The type of the resource instance.</typeparam>
	/// <param name="_resourceKey">A unique identifier key for the resource.</param>
	/// <param name="_outHandle">Outputs a handle for the resource, or null, if the resource could not be found, or if its type was incorrect.</param>
	/// <returns>True if a resource handle was found and of the requested type, false otherwise.</returns>
	public bool GetResourceHandle<T>(string _resourceKey, [NotNullWhen(true)] out ResourceHandle<T>? _outHandle) where T : class
	{
		if (!GetResourceHandle(_resourceKey, out ResourceHandle? handle))
		{
			_outHandle = null;
			return false;
		}
		if (handle is not ResourceHandle<T> typedHandle)
		{
			logger.LogError($"Incorrect data type for resource key '{_resourceKey}'! (Expected: {typeof(T).Name}, Found handle: '{handle}')");
			_outHandle = null;
			return false;
		}

		_outHandle = typedHandle;
		return true;
	}

	internal bool LoadResource(ResourceHandle _handle, bool _loadImmediately, FuncAssignLoadedResource _funcAssignResourceCallback)
	{
		ArgumentNullException.ThrowIfNull(_handle);
		ArgumentNullException.ThrowIfNull(_funcAssignResourceCallback);

		if (IsDisposed)
		{
			logger.LogError("Cannot load resource using disposed resource service!");
			return false;
		}
		if (!_handle.IsValid())
		{
			return false;
		}

		// If the resource is already loaded, do nothing:
		if (_handle.IsLoaded)
		{
			return true;
		}

		// If background loading has already been initiated:
		ResourceLoadHandle? loadingHandle = null;
		if (_handle.LoadingState == ResourceLoadingState.Pending)
		{
			// A) Resource is still queued up and waiting:
			if (queue.GetLoadHandle(_handle, out loadingHandle))
			{
				if (!AbortLoading(_handle))
				{
					logger.LogError("Failed to remove resource from background loading queue!");
					return false;
				}
			}
			// B): Resource is actively being imported:
			else
			{
				const int waitIntervalMs = 3;
				int timeout = ResourceConstants.waitForLoadingToCompleteTimeoutMs;
				while (_handle.LoadingState < ResourceLoadingState.Loaded && timeout > 0)
				{
					Thread.Sleep(waitIntervalMs);
					timeout -= waitIntervalMs;
				}
			}
		}

		if (_handle.IsLoaded)
		{
			return true;
		}

		// If resource is needed immediately, load it synchronously on the current thread:
		if (_loadImmediately)
		{
			return LoadImmediately(_handle, _funcAssignResourceCallback);
		}

		// Queue resource up for background loading:
		loadingHandle = new(_handle, _funcAssignResourceCallback);

		if (!queue.Enqueue(loadingHandle))
		{
			logger.LogError("Failed to queue up resource for background loading!");
			return false;
		}
		return true;
	}

	internal async Task<bool> LoadResourceAsync(ResourceHandle _handle, FuncAssignLoadedResource _funcAssignResourceCallback)
	{
		ArgumentNullException.ThrowIfNull(_handle);
		ArgumentNullException.ThrowIfNull(_funcAssignResourceCallback);

		if (IsDisposed)
		{
			logger.LogError("Cannot load resource asynchronously using disposed resource service!");
			return false;
		}
		if (!_handle.IsValid())
		{
			return false;
		}

		// If the resource is already loaded, do nothing:
		if (_handle.IsLoaded)
		{
			return true;
		}

		ResourceLoadHandle? loadingHandle = null;

		// Not loaded? Queue resource up for background loading:
		if (_handle.LoadingState == ResourceLoadingState.NotLoaded)
		{
			loadingHandle = new(_handle, _funcAssignResourceCallback);

			if (!queue.Enqueue(loadingHandle))
			{
				logger.LogError("Failed to queue up resource for background loading!");
				return false;
			}
		}
		// Already in loading process:
		else if (_handle.LoadingState == ResourceLoadingState.Pending)
		{
			// Check if resource is still in queue:
			if (!queue.GetLoadHandle(_handle, out loadingHandle))
			{
				// If resource is actively being imported, wait for completion:
				const int waitIntervalMs = 3;
				int timeout = ResourceConstants.waitForLoadingToCompleteTimeoutMs;
				while (_handle.LoadingState < ResourceLoadingState.Loaded && timeout > 0)
				{
					await Task.Delay(waitIntervalMs);
					timeout -= waitIntervalMs;
				}
			}
		}

		// Wait for the loading process to finish:
		if (loadingHandle is not null)
		{
			await loadingHandle.WaitTask;
		}

		// Report success:
		return _handle.IsLoaded;
	}

	private bool LoadImmediately(ResourceHandle _handle, FuncAssignLoadedResource _funcAssignResourceCallback)
	{
		ArgumentNullException.ThrowIfNull(_handle);
		ArgumentNullException.ThrowIfNull(_funcAssignResourceCallback);

		Debug.Assert(!IsDisposed, "Resource service that has already been disposed!");

		// Retrieve the location, format, and type of the resource:
		if (!resourceDataService.GetResourceData(_handle.ResourceKey, out ResourceData? resourceData))
		{
			logger.LogError($"Failed to retrieve resource file data for resource '{_handle.ResourceKey}'!");
			return false;
		}

		// Try to find the most fitting import service for this resource's format and type:
		IImportService? selectedImporter = null;
		foreach (IImportService importService in importServices)
		{
			if (!importService.IsResourceFormatKeySupported(resourceData.FormatKey, ResourceOperationType.Import))
			{
				continue;
			}
			ResourceTypeSupport supportLevel = importService.IsResourceTypeSupported(resourceData.Type, resourceData.SubType);
			if (supportLevel == ResourceTypeSupport.None)
			{
				continue;
			}
			
			selectedImporter = importService;
			if (supportLevel == ResourceTypeSupport.SubTypeSupported)
			{
				break;
			}
		}

		if (selectedImporter is null)
		{
			logger.LogError($"No fitting importer could be found for resource '{_handle.ResourceKey}'");
			return false;
		}

		// try importing the resource:
		if (!selectedImporter.ImportResourceData(in resourceData, out object? resourceInstance))
		{
			return false;
		}

		// use callback to assign resource instance to its handle:
		_funcAssignResourceCallback(resourceInstance);
		return true;
	}

	/// <summary>
	/// Aborts background loading of a resource.
	/// </summary>
	/// <remarks>
	/// Note: This will remove a resource from the queue before it is loaded in the background, but it will not
	/// interrupt an actively ongoing import on that thread. If the resource is in that stage, it will finish
	/// loading anyways. Remember to dispose your resource handle if the resource is no longer needed.
	/// </remarks>
	/// <param name="_handle">The resource whose background loading you wish to abort.</param>
	/// <returns>True if any ongoing loading process on the background thread has been aborted, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource handle may not be null.</exception>
	public bool AbortLoading(ResourceHandle _handle)
	{
		ArgumentNullException.ThrowIfNull(_handle);

		if (IsDisposed)
		{
			logger.LogError("Cannot abort of resource resource service that has already been disposed!");
			return false;
		}

		if (!queue.Contains(_handle))
		{
			return true;
		}

		if (!queue.Remove(_handle))
		{
			logger.LogError("Failed to removed resource from background loading queue!");
			return false;
		}

		return true;
	}

	internal bool GetResourceData(string _resourceKey, [NotNullWhen(true)] out ResourceData? _outData)
	{
		ArgumentNullException.ThrowIfNull(_resourceKey);

		if (IsDisposed)
		{
			logger.LogError("Cannot get resource data using disposed resource service!");
			_outData = null;
			return false;
		}

		return resourceDataService.GetResourceData(_resourceKey, out _outData);
	}

	/// <summary>
	/// Registers a new import service that may be used by the resource system to import resources.
	/// </summary>
	/// <param name="_newImportService">The new resource service, providing support for additional resource types and sub-types,
	/// or adding support for new file formats. May not be null or disposed.</param>
	/// <returns>True if the service was registered, false if registration failed or if it was already registered.</returns>
	/// <exception cref="ArgumentNullException">New import service may not be null.</exception>
	/// <exception cref="ObjectDisposedException">New import service may not disposed.</exception>
	public bool RegisterImportService(IImportService _newImportService)
	{
		ArgumentNullException.ThrowIfNull(_newImportService);
		ObjectDisposedException.ThrowIf(_newImportService is IExtendedDisposable { IsDisposed: true }, _newImportService);

		if (IsDisposed)
		{
			logger.LogError("Cannot register new import service with disposed resource service!");
			return false;
		}

		if (!importServices.Add(_newImportService))
		{
			logger.LogError($"Resource import service '{_newImportService}' has already been registered!");
			return false;
		}

		logger.LogMessage($"Registered new resource import service: '{_newImportService.GetType().Name}'");
		return true;
	}

	#endregion
}
