using FragEngine.Interfaces;

namespace FragEngine.Scenes;

/// <summary>
/// Interface for classes that provide a pose in 3D space.
/// </summary>
/// <remarks>
/// This interface exposes the <see cref="IChecksumVersioned"/> property, such that checksum reflects the state of the pose.
/// If the pose, value, or transformation of the pose source changes, its checksum must also change to a different value. Most pose
/// source implementations may use simple incremental checksums.
/// </remarks>
public interface IPoseSource
{
	#region Properties

	/// <summary>
	/// Gets a checksum or version number for the current pose value.
	/// If the pose provided by this pose source changes, the checksum will also change, allowing you to respond to the changed pose.
	/// </summary>
	ulong PoseChecksum { get; }

	#endregion
	#region Methods

	/// <summary>
	/// Gets the current pose, in local space.
	/// </summary>
	/// <returns>A pose.</returns>
	Pose GetLocalPose();

	/// <summary>
	/// Gets the current pose, in world space.
	/// </summary>
	/// <returns>A pose.</returns>
	Pose GetWorldPose();

	#endregion
}
