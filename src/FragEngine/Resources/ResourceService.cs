using FragEngine.EngineCore;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using FragEngine.Resources.Internal;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources;

public sealed class ResourceService : IExtendedDisposable
{
	#region Fields

	private readonly ILogger logger;
	private readonly PlatformService platformService;

	private readonly ResourceLoadQueue queue;
	private readonly Thread backgroundLoadThread;
	private readonly CancellationTokenSource loadCancellationTokenSrc = new();

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	public ResourceService(ILogger _logger, PlatformService _platformService)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_platformService);

		logger = _logger;
		platformService = _platformService;

		logger.LogStatus("# Initializing resource service.");

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

		if (!GetResourceData(_handle.ResourceKey, out ResourceData? resourceData))
		{
			logger.LogError($"Failed to retrieve resource file data for resource '{_handle.ResourceKey}'!");
			return false;
		}


		//TODO [IMPORTANT]: Add actual import logic here.


		return true;	//TEMP
	}

	/// <summary>
	/// Aborts background loading of a resource.
	/// </summary>
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

		//TODO

		_outData = null;	//TEMP
		return false;
	}

	#endregion
}
