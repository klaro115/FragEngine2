namespace FragEngine.Graphics.Buffers;

public record struct DeviceBufferDownloadResult<T>(T[] DownloadedData, uint DataCount, bool IsSuccess) where T : unmanaged
{
	/// <summary>
	/// Gets a failed download result containing no data.
	/// </summary>
	public static DeviceBufferDownloadResult<T> Failure => new([], 0u, false);
}
