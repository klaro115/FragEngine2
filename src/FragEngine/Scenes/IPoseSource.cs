namespace FragEngine.Scenes;

/// <summary>
/// Interface for classes that provide a pose in 3D space.
/// </summary>
public interface IPoseSource
{
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
