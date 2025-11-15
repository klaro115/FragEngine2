
namespace FragEngine.Scenes;

/// <summary>
/// Class that provides a pose in 3D space. The pose is stored locally, and may be updated manually via the <see cref="CurrentPose"/> property.
/// </summary>
public sealed class ConstantPoseSource : IPoseSource
{
	#region Fields

	private Pose currentPose;

	#endregion
	#region Properties

	/// <summary>
	/// Gets or sets the current pose.
	/// The <see cref="PoseChecksum"/> value will be incremented whenever this setter is invoked.
	/// </summary>
	public Pose CurrentPose
	{
		get => currentPose;
		set
		{
			PoseChecksum++;
			currentPose = value;
		}
	}

	public ulong PoseChecksum { get; private set; }

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new constant-backed pose source.
	/// </summary>
	/// <param name="_initialPose">The pose value from which to initialize this source.</param>
	public ConstantPoseSource(Pose _initialPose)
	{
		currentPose = _initialPose;
		PoseChecksum = (ulong)currentPose.GetHashCode();
	}

	#endregion
	#region Methods

	public Pose GetLocalPose() => currentPose;
	public Pose GetWorldPose() => currentPose;

	#endregion
}
