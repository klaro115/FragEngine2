using FragEngine.Interfaces;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.Graphics.Buffers;

/// <summary>
/// Container type for downloading the contents of a <see cref="DeviceBuffer"/> on the GPU to CPU.side memory.
/// An instance of this type may be used repeatedly, and only needs to be disposed if you don't need any further
/// GPU readbacks.
/// </summary>
/// <typeparam name="T">The type each data element in the GPU buffer. Must be an unmanaged struct type with
/// a known byte size.</typeparam>
public sealed class DeviceBufferDownload<T> : IExtendedDisposable where T : unmanaged
{
	#region Types

	public enum DownloadStatus
	{
		NotStarted,
		Busy,
		Success,
		Failure,
	}

	private sealed class DownloadRequest(DeviceBufferDownload<T> _download) : IGraphicsResourceDownloadRequest
	{
		private readonly DeviceBufferDownload<T> download = _download;

		public bool IsDisposed => download.IsDisposed;

		public void Dispose() { }
		public bool IsValid() => !IsDisposed;
		public bool ScheduleCopy(CommandList _cmdList) => download.OnCopyData(_cmdList);
		public bool DownloadData() => download.OnDownloadData();
	}

	#endregion
	#region Fields

	private readonly GraphicsService graphicsService;
	private readonly ILogger logger;

	private readonly T[] downloadedData;
	private readonly uint elementByteSize;
	private readonly DeviceBuffer stagingBuffer;

	private uint startIndex;
	private uint elementCount;
	private DeviceBuffer? srcBuffer = null;
	private TaskCompletionSource<DeviceBufferDownloadResult<T>>? completionSource = null;
	private FuncBufferDownloadCompleted<T>? funcDownloadCompletedCallback = null;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;
	public DownloadStatus Status { get; private set; } = DownloadStatus.NotStarted;

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new GPU buffer download.
	/// </summary>
	/// <param name="_graphicsService">The engine's graphics service singleton.</param>
	/// <param name="_logger">The engine's logging service singleton.</param>
	/// <param name="_maxCapacity">The maximum number of elements in the buffer, must be at least 1.</param>
	/// <param name="_elementByteSize">The byte size (or stride, if multiple elements) of each element in the buffer.</param>
	/// <exception cref="ArgumentException"><paramref name="_maxCapacity"/> may not be zero, and <paramref name="_elementByteSize"/>
	/// may not be smaller than 4 bytes.</exception>
	/// <exception cref="ArgumentNullException">Graphics service and logger may not be null.</exception>
	/// <exception cref="Exception">Failure to create staging buffer.</exception>
	/// <exception cref="ObjectDisposedException">Graphics service may not be disposed.</exception>
	public DeviceBufferDownload(GraphicsService _graphicsService, ILogger _logger, uint _maxCapacity, uint _elementByteSize)
	{
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ArgumentNullException.ThrowIfNull(_logger);
		ObjectDisposedException.ThrowIf(_graphicsService.IsDisposed, _graphicsService);
		
		if (_maxCapacity == 0)
		{
			throw new ArgumentException("Maximum download buffer capacity may not be zero!", nameof(_maxCapacity));
		}
		if (_elementByteSize < 4)
		{
			throw new ArgumentException("Element byte size must be at least 4 bytes!", nameof(_elementByteSize));
		}

		graphicsService = _graphicsService;
		logger = _logger;

		downloadedData = new T[_maxCapacity];
		elementByteSize = _elementByteSize;

		uint totalByteSize = _maxCapacity * elementByteSize;
		BufferDescription bufferDesc = new(totalByteSize, BufferUsage.Staging);

		try
		{
			stagingBuffer = _graphicsService.ResourceFactory.CreateBuffer(ref bufferDesc);
			stagingBuffer.Name = $"BufStaging_{nameof(DeviceBufferDownload<T>)}_Capacity={_maxCapacity}_ElemSize={elementByteSize}";
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to create staging buffer for {nameof(DeviceBufferDownload<T>)}!", ex);
		}
	}

	~DeviceBufferDownload()
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
		IsDisposed = true;
		stagingBuffer.Dispose();

		if (Status == DownloadStatus.Busy)
		{
			SetDownloadFailed();
		}
		if (_disposing)
		{
			completionSource = null;
			funcDownloadCompletedCallback = null;
		}
	}

	/// <summary>
	/// Requests a new download of GPU buffer contents.
	/// </summary>
	/// <param name="_srcBuffer">The source buffer whose data to download. May not be disposed.</param>
	/// <param name="_elementCount">The number of elements to download from the start of the buffer.
	/// The download process will complete immediately if this is zero.</param>
	/// <param name="_funcDownloadCompletedCallback">Optional. A callback method to invoke when data has
	/// finished downloading and is ready for use. If null, download results can instead be queried using
	/// '<see cref="GetDownloadedData"/>'.</param>
	/// <returns>True if the download was started successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Command list and source buffer may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Command list and source buffer may not be disposed.</exception>
	public bool RequestDownload(DeviceBuffer _srcBuffer, uint _startIndex, uint _elementCount, FuncBufferDownloadCompleted<T>? _funcDownloadCompletedCallback = null)
	{
		ArgumentNullException.ThrowIfNull(_srcBuffer);
		ObjectDisposedException.ThrowIf(_srcBuffer.IsDisposed, _srcBuffer);

		if (IsDisposed)
		{
			logger.LogError($"Cannot download GPU buffer using disposed {nameof(DeviceBufferDownload<T>)}!");
			return false;
		}
		if (Status == DownloadStatus.Busy)
		{
			logger.LogError($"Cannot download GPU buffer, {nameof(DeviceBufferDownload<T>)} is still busy!");
			return false;
		}

		srcBuffer = _srcBuffer;
		startIndex = _startIndex;
		elementCount = _elementCount;
		funcDownloadCompletedCallback = _funcDownloadCompletedCallback;
		completionSource = null;

		Status = DownloadStatus.Busy;

		DownloadRequest request = new(this);

		if (!graphicsService.RequestResourceDownload(request))
		{
			logger.LogError($"Failed to schedule {nameof(DeviceBufferDownload<T>)}!");
			Status = DownloadStatus.Failure;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Requests a new download of GPU buffer contents, and awaits completion.
	/// </summary>
	/// <param name="_srcBuffer">The source buffer whose data to download. May not be disposed.</param>
	/// <param name="_elementCount">The number of elements to download from the start of the buffer.
	/// The download process will complete immediately if this is zero.</param>
	/// <returns>A results object containing downloaded data, and indicating success or failure of the process.
	/// Check '<see cref="DeviceBufferDownloadResult{T}.IsSuccess"/>' to see if the download was successful.</returns>
	/// <exception cref="ArgumentNullException">Command list and source buffer may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Command list and source buffer may not be disposed.</exception>
	/// <exception cref="TaskCanceledException">Async download task was cancelled, or download instance was disposed
	/// while awaiting completion.</exception>
	public async Task<DeviceBufferDownloadResult<T>> RequestDownloadAsync(DeviceBuffer _srcBuffer, uint _startIndex, uint _elementCount)
	{
		ArgumentNullException.ThrowIfNull(_srcBuffer);
		ObjectDisposedException.ThrowIf(_srcBuffer.IsDisposed, _srcBuffer);

		if (IsDisposed)
		{
			logger.LogError($"Cannot download GPU buffer using disposed {nameof(DeviceBufferDownload<T>)}!");
			return DeviceBufferDownloadResult<T>.Failure;
		}
		if (Status == DownloadStatus.Busy)
		{
			logger.LogError($"Cannot download GPU buffer, {nameof(DeviceBufferDownload<T>)} is still busy!");
			return DeviceBufferDownloadResult<T>.Failure;
		}

		srcBuffer = _srcBuffer;
		startIndex = _startIndex;
		elementCount = _elementCount;
		funcDownloadCompletedCallback = null;
		completionSource = new();

		Status = DownloadStatus.Busy;

		DownloadRequest request = new(this);

		if (!graphicsService.RequestResourceDownload(request))
		{
			logger.LogError($"Failed to schedule {nameof(DeviceBufferDownload<T>)}!");
			Status = DownloadStatus.Failure;
			return DeviceBufferDownloadResult<T>.Failure;
		}

		DeviceBufferDownloadResult<T> result = await completionSource.Task;
		return result;
	}

	private bool OnCopyData(CommandList _cmdList)
	{
		if (srcBuffer!.IsDisposed)
		{
			logger.LogError($"Source buffer '{srcBuffer.Name}' of {nameof(DeviceBufferDownload<T>)} has been disposed! Aborting download.");
			return false;
		}

		uint startOffset = startIndex *	elementByteSize;
		uint totalByteSize = elementCount * elementByteSize;

		try
		{
			_cmdList.CopyBuffer(srcBuffer, startOffset, stagingBuffer, 0, totalByteSize);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to schedule GPU buffer copy for {nameof(DeviceBufferDownload<T>)}!", ex);
			return false;
		}
	}

	private bool OnDownloadData()
	{
		try
		{
			var mapped = graphicsService.Device.Map<T>(stagingBuffer, MapMode.Read);
			for (uint i = 0; i < elementCount; ++i)
			{
				downloadedData[i] = mapped[i];
			}
			graphicsService.Device.Unmap(stagingBuffer);

			SetDownloadCompleted();
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to download contents of staging buffer for {nameof(DeviceBufferDownload<T>)}!", ex);

			SetDownloadFailed();
			return false;
		}
	}

	private void SetDownloadCompleted()
	{
		Status = DownloadStatus.Success;

		DeviceBufferDownloadResult<T> result = new(downloadedData, elementCount, true);

		funcDownloadCompletedCallback?.Invoke(result);
		if (completionSource is not null && !completionSource.Task.IsCompleted)
		{
			completionSource?.SetResult(result);
		}
	}

	private void SetDownloadFailed()
	{
		Status = DownloadStatus.Failure;

		funcDownloadCompletedCallback?.Invoke(DeviceBufferDownloadResult<T>.Failure);
		if (completionSource is not null && !completionSource.Task.IsCompleted)
		{
			completionSource?.SetCanceled();
		}
	}

	/// <summary>
	/// Get the results of the last download.
	/// </summary>
	/// <returns>The result of the last download. If no download has completed yet, or the download is still in progress,
	/// or the download resulted in failure, this will return '<see cref="DeviceBufferDownloadResult{T}.Failure"/>' instead.
	/// Check '<see cref="DeviceBufferDownloadResult{T}.IsSuccess"/>', to see if the returned data is valid.</returns>
	public DeviceBufferDownloadResult<T> GetDownloadedData()
	{
		if (IsDisposed)
		{
			logger.LogError($"Cannot retrieve downloaded data from disposed {nameof(DeviceBufferDownload<T>)}!");
			return DeviceBufferDownloadResult<T>.Failure;
		}

		if (Status != DownloadStatus.Success)
		{
			logger.LogError($"Cannot retrieve downloaded data; {nameof(DeviceBufferDownload<T>)} status does not indicate success!", LogEntrySeverity.Trivial);
			return DeviceBufferDownloadResult<T>.Failure;
		}

		DeviceBufferDownloadResult<T> result = new(downloadedData, elementCount, true);
		return result;
	}

	#endregion
}
