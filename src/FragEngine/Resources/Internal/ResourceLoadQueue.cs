using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Internal;

/// <summary>
/// A thread-safe priority queue for loading resources in the background.
/// </summary>
/// <param name="_logger">The engine's logging service singleton.</param>
/// <exception cref="ArgumentNullException">Logging service may not be null.</exception>
internal sealed class ResourceLoadQueue(ILogger _logger) : IExtendedDisposable
{
	#region Fields

	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

	private readonly List<ResourceLoadHandle> list = new(ResourceConstants.loadingQueueStartingCapacity);

	private int minPriorityInQueue = ResourceConstants.defaultResourceLoadPriority;
	private int maxPriorityInQueue = ResourceConstants.defaultResourceLoadPriority;

	private readonly ReaderWriterLockSlim readWriteLock = new();

	#endregion
	#region Constants

	private const int readWriteLockTimeout = 100;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	~ResourceLoadQueue()
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

	private void Dispose(bool _disposing)
	{
		if (!IsDisposed && _disposing)
		{
			Clear();
		}

		IsDisposed = true;

		readWriteLock.Dispose();
	}

	/// <summary>
	/// Removes all pending resources from the queue.
	/// </summary>
	public void Clear()
	{
		if (IsDisposed || readWriteLock.TryEnterWriteLock(readWriteLockTimeout))
		{
			logger.LogError("Failed to acquire lock for clearing resource load queue!");
			return;
		}

		list.Clear();

		readWriteLock.ExitWriteLock();
	}

	/// <summary>
	/// Checks if a resource already exists on the queue.
	/// </summary>
	/// <param name="_resourceHandle">A handle to the resource.</param>
	/// <returns>True if the resource is queued up, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource handle may not be null.</exception>
	public bool Contains(ResourceHandle _resourceHandle)
	{
		ArgumentNullException.ThrowIfNull(_resourceHandle);

		Debug.Assert(!IsDisposed, "Cannot check if resource is queued up on disposed queue!");

		if (!readWriteLock.TryEnterReadLock(readWriteLockTimeout))
		{
			logger.LogError("Failed to acquire lock for finding resource in loading queue!");
			return false;
		}

		try
		{
			bool contains = list.Any(o => o.ResourceHandle == _resourceHandle);
			return contains;
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Tries to get the loading handle of a resource on the queue.
	/// </summary>
	/// <param name="_resourceHandle">A handle to the resource.</param>
	/// <param name="_outLoadHandle">Outputs the resource's loading handle if it is queued up, or null, if the resource is not in the queue.</param>
	/// <returns>True if the resource's loading handle was found, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource handle may not be null.</exception>
	public bool GetLoadHandle(ResourceHandle _resourceHandle, [NotNullWhen(true)] out ResourceLoadHandle? _outLoadHandle)
	{
		ArgumentNullException.ThrowIfNull(_resourceHandle);

		Debug.Assert(!IsDisposed, "Cannot get load handle from disposed queue!");

		if (!readWriteLock.TryEnterReadLock(readWriteLockTimeout))
		{
			logger.LogError("Failed to acquire lock for finding resource in loading queue!");
			_outLoadHandle = null;
			return false;
		}

		try
		{
			_outLoadHandle = list.FirstOrDefault(o => o.ResourceHandle == _resourceHandle);
			return _outLoadHandle is not null && !_outLoadHandle.IsDisposed;
		}
		finally
		{
			readWriteLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Tries to add a resource to the queue.
	/// </summary>
	/// <param name="_newLoadHandle">A loading handle for the new resource we wish to add.</param>
	/// <returns>True if the resource could queued up, false otherwise.</returns>
	public bool Enqueue(ResourceLoadHandle _newLoadHandle)
	{
		ArgumentNullException.ThrowIfNull(_newLoadHandle);

		Debug.Assert(!_newLoadHandle.IsDisposed, "Resource load handle has already been disposed!");
		Debug.Assert(!IsDisposed, "Cannot enqueue resource for loading on disposed queue!");

		if (!readWriteLock.TryEnterUpgradeableReadLock(readWriteLockTimeout))
		{
			logger.LogError("Failed to acquire lock for adding resource to loading queue!");
			return false;
		}

		try
		{
			// Exit here, if the resource is already queues up:
			if (list.Any(o => o == _newLoadHandle || o.ResourceHandle == _newLoadHandle.ResourceHandle))
			{
				return true;
			}

			readWriteLock.EnterWriteLock();
			try
			{
				// Insert handle at the appropriate priority level:
				if (list.Count == 0 || _newLoadHandle.Priority >= maxPriorityInQueue)
				{
					list.Add(_newLoadHandle);
					maxPriorityInQueue = _newLoadHandle.Priority;
				}
				else if (_newLoadHandle.Priority < minPriorityInQueue)
				{
					list.Insert(0, _newLoadHandle);
					minPriorityInQueue = _newLoadHandle.Priority;
				}
				else
				{
					int insertIdx = 0;
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].Priority > _newLoadHandle.Priority)
						{
							insertIdx = i;
							break;
						}
					}
					list.Insert(insertIdx, _newLoadHandle);
				}
				return true;
			}
			// Exit locks:
			finally
			{
				readWriteLock.ExitWriteLock();
			}
		}
		finally
		{
			readWriteLock.ExitUpgradeableReadLock();
		}
	}

	/// <summary>
	/// Tries to pop the highest priority resource from the queue.
	/// </summary>
	/// <param name="_outLoadHandle">Outputs the highest priority resource, or null, if the queue is empty.</param>
	/// <returns>True if a resource could be dequeued, false if the queue is empty or on error.</returns>
	public bool Dequeue([NotNullWhen(true)] out ResourceLoadHandle? _outLoadHandle)
	{
		Debug.Assert(!IsDisposed, "Cannot dequeue resource from disposed queue!");

		if (!readWriteLock.TryEnterWriteLock(readWriteLockTimeout))
		{
			logger.LogError("Failed to acquire lock for dequeueing resource from loading queue!");
			_outLoadHandle = null;
			return false;
		}

		try
		{
			// List is empty? Nothing to dequeue:
			if (list.Count == 0)
			{
				_outLoadHandle = null;
				return false;
			}

			// Remove first element from the list:
			_outLoadHandle = list[0];
			list.RemoveAt(0);
			return true;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Removes a resource from the queue.
	/// </summary>
	/// <param name="_resourceHandle">A handle to the resource we wish to remove from the queue.</param>
	/// <returns>True if the resource was found in the queue and removed successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource handle may not be null.</exception>
	public bool Remove(ResourceHandle _resourceHandle)
	{
		ArgumentNullException.ThrowIfNull(_resourceHandle);

		Debug.Assert(!IsDisposed, "Cannot remove resource from disposed queue!");

		if (!readWriteLock.TryEnterWriteLock(readWriteLockTimeout))
		{
			logger.LogError("Failed to acquire lock for removing resource from loading queue!");
			return false;
		}

		try
		{
			int removedCount = list.RemoveAll(o => o.ResourceHandle == _resourceHandle);
			return removedCount > 0;
		}
		finally
		{
			readWriteLock.ExitWriteLock();
		}
	}

	#endregion
}
