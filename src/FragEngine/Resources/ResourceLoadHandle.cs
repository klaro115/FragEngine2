using FragEngine.Interfaces;
using FragEngine.Resources.Enums;

namespace FragEngine.Resources;

/// <summary>
/// A handle for tracking the loading progress of a resource that is loaded asynchronously in a background thread.
/// </summary>
/// <param name="_resourceHandle">A handle of the resource that's being loaded.</param>
/// <param name="_funcAssignResourceCallback">A callback function for assigning the loaded value to the resource handle after completion.</param>
internal sealed class ResourceLoadHandle(ResourceHandle _resourceHandle, FuncAssignLoadedResource _funcAssignResourceCallback) : IExtendedDisposable
{
	#region Fields

	private TaskCompletionSource<bool>? completionSource = null;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// A handle of the resource that's being loaded.
	/// </summary>
	public ResourceHandle ResourceHandle { get; } = _resourceHandle;

	/// <summary>
	/// A callback function for assigning the loaded value to the resource handle after completion.
	/// </summary>
	public FuncAssignLoadedResource AssignResourceCallback { get; } = _funcAssignResourceCallback;

	/// <summary>
	/// Gets the priority rating for loading this resource. Lower values are loaded first.
	/// </summary>
	public int Priority { get; init; } = ResourceConstants.defaultResourceLoadPriority;

	/// <summary>
	/// Gets the current state of the loading process.
	/// </summary>
	public ResourceLoadingState LoadingState { get; private set; } = ResourceLoadingState.Pending;

	/// <summary>
	/// Gets an awaitable task that finishes when loading either completes or fails.
	/// </summary>
	public Task WaitTask
	{
		get
		{
			if (LoadingState is ResourceLoadingState.Loaded or ResourceLoadingState.FailedToLoad)
			{
				return Task.CompletedTask;
			}

			completionSource ??= new();
			return completionSource.Task;
		}
	}

	#endregion
	#region Constructors

	~ResourceLoadHandle()
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

		completionSource?.SetException(new ObjectDisposedException("Resource load handle has been disposed!"));
		//...
	}

	/// <summary>
	/// Sets the result state of the resource's loading process.
	/// </summary>
	/// <param name="_wasLoadedSuccessfully">Whether the resource was loaded successfully.</param>
	public void SetLoadingResult(bool _wasLoadedSuccessfully)
	{
		if (IsDisposed)
		{
			return;
		}

		if (_wasLoadedSuccessfully)
		{
			LoadingState = ResourceLoadingState.Loaded;
		}
		else
		{
			LoadingState = ResourceLoadingState.FailedToLoad;
		}

		completionSource?.SetResult(_wasLoadedSuccessfully);
	}

	#endregion
}
