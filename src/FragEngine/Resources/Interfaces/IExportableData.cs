using FragEngine.Logging;

namespace FragEngine.Resources.Interfaces;

/// <summary>
/// Interface for data structs that can be exported or serialized to a resource stream.
/// </summary>
public interface IExportableData
{
	/// <summary>
	/// Tries to write this data structure to stream.
	/// </summary>
	/// <param name="_writer">A binary writer that writes to a resource stream.</param>
	/// <param name="_logger">Optional. A logger for outputting errors.</param>
	/// <returns>True if the data was written successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Binary writer may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Logger has already been disposed.</exception>
	bool Write(BinaryWriter _writer, ILogger? _logger);
}
