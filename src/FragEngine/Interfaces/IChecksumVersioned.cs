namespace FragEngine.Interfaces;

/// <summary>
/// Interface for classes that expose a checksum through which they can be compared and versioned.
/// If the checksum of an instance differs from another instance, then their contents must be different.
/// The checksum may also be used as a "versioning" feature, to check if a newer version of a piece of
/// data has arrived.
/// </summary>
public interface IChecksumVersioned
{
	#region Properties

	/// <summary>
	/// Gets a checksum through which instances of this object can be compared, or that may be used to
	/// check if a newer version of the data has arrived.
	/// </summary>
	/// <remarks>
	/// NOTE: Unless otherwise specified by implementations of the '<see cref="IChecksumVersioned"/>'
	/// interface, do not assume that "versioning" checksums change in an incremental manner.
	/// </remarks>
	ulong Checksum { get; }

	#endregion
}
