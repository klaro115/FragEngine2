namespace FragEngine.Graphics.Cameras;

/// <summary>
/// Enumeration of different camera projection types.
/// </summary>
public enum CameraProjectionType
{
	/// <summary>
	/// The camera produces a perspective image, using a pyramid-shaped viewport frustum.
	/// Objects grow smaller with increaing distance from the camera.
	/// </summary>
	Perspective,
	/// <summary>
	/// The camera produces an orthotographics image, using a rectangular viewport frustum.
	/// Objects stay that same size no matter how far they are from the camera.
	/// </summary>
	Orthographic,
}
