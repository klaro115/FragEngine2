using FragEngine.Interfaces;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// Interface for requests to download buffer or texture contents from GPU memory.
/// Download requests may be queued up in the graphics service using '<see cref="GraphicsService.RequestResourceDownload(IGraphicsResourceDownloadRequest)"/>'.
/// </summary>
internal interface IGraphicsResourceDownloadRequest : IExtendedDisposable, IValidated
{
	#region Methods

	/// <summary>
	/// Tries to schedule the copying of data from the GPU resource (a buffer or texture) to a staging resource.
	/// </summary>
	/// <param name="_cmdList">A command list on which the buffer/texture copy will be queued up.</param>
	/// <returns>True if teh copy was scheduled successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Command list may not be null.</exception>
	bool ScheduleCopy(CommandList _cmdList);

	/// <summary>
	/// Tries to map the staging resource and copy its contents to CPU-side memory.
	/// </summary>
	/// <returns>True if the data was downloaded successfully, false otherwise.</returns>
	bool DownloadData();

	#endregion
}
