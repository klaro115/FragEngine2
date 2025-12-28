namespace FragEngine.Graphics.Buffers;

/// <summary>
/// Method delegate for callback function when a GPU buffer download is completed.
/// </summary>
/// <typeparam name="T">Type of the data inside the buffer. Must be an unmanaged struct type.</typeparam>
/// <param name="_result">A result object containing the downloaded data.</param>
public delegate void FuncBufferDownloadCompleted<T>(DeviceBufferDownloadResult<T> _result) where T : unmanaged;
