using Veldrid;

namespace FragEngine.Graphics.Buffers;

/// <summary>
/// Structure containing the result of downloading the contents of a '<see cref="DeviceBuffer"/>' on the GPU
/// to an array in CPU-side memory.
/// </summary>
/// <typeparam name="T">The type each data element in the GPU buffer. Must be an unmanaged struct type.</typeparam>
/// <param name="DownloadedData">An array containing the downloaded data. The array's size may be larger than
/// the number of data elements that were downloaded, and may be re-used for future downloads. Do not keep any
/// references to this instance. If unsuccessful, this may be an empty array.</param>
/// <param name="DataCount">The total number of elements that were downloaded from the '<see cref="DeviceBuffer"/>'.
/// If unsuccessful, this will be 0.</param>
/// <param name="IsSuccess">Gets whether the download request was completed successfully.</param>
public record struct DeviceBufferDownloadResult<T>(T[] DownloadedData, uint DataCount, bool IsSuccess) where T : unmanaged
{
	/// <summary>
	/// Gets a failed download result containing no data.
	/// </summary>
	public static DeviceBufferDownloadResult<T> Failure => new([], 0u, false);
}
