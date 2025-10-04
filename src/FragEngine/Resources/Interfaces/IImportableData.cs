using FragEngine.Logging;

namespace FragEngine.Resources.Interfaces;

/// <summary>
/// Interface for data structs that can be imported or parsed from a resource stream.
/// </summary>
/// <typeparam name="T">The type of the data struct. Usually, this is the type that implements this interface.</typeparam>
public interface IImportableData<T> where T : struct
{
	#region Methods

	/// <summary>
	/// Tries to read a data structure from stream.
	/// </summary>
	/// <param name="_reader">A binary reader that reads from a resource stream.</param>
	/// <param name="_logger">Optional. A logger for outputting errors.</param>
	/// <param name="_outData">Outputs the fully read and parsed data structure.</param>
	/// <returns>True if the data was read successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Binary reader may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Logger has already been disposed.</exception>
	static abstract bool Read(BinaryReader _reader, ILogger? _logger, out T _outData);

	#endregion
}
