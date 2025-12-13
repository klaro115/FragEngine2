using FragEngine.Graphics.ConstantBuffers;
using Veldrid;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// Constants used by the <see cref="Camera"/> class and related types.
/// </summary>
public static class CameraConstants
{
	#region Constants General

	/// <summary>
	/// Initial value for invalid or uninitialized checksums. If a checksum has this value, it needs to be
	/// recalculated, or no settings have been applied yet.
	/// </summary>
	internal const ulong UNINITIALIZED_CHECKSUM = 0ul;

	#endregion
	#region Constants Projection

	// PROJECTION:

	/// <summary>
	/// The default vertical opening angle of a perspective camera's viewport frustum, in degrees.
	/// </summary>
	public const float defaultFieldOfViewDegrees = 60.0f;
	/// <summary>
	/// The default distance of a camera's near clipping plane, in meters.
	/// </summary>
	public const float defaultNearClipPlane = 0.1f;
	/// <summary>
	/// The default distance of a camera's far clipping plane, in meters.
	/// </summary>
	public const float defaultFarClipPlane = 1000.0f;
	/// <summary>
	/// The default height of an orthographic camera's viewport box, in meters.
	/// </summary>
	public const float defaultOrthographicSize = 5.0f;

	#endregion
	#region Constants Output

	// OUTPUT:

	/// <summary>
	/// The default horizontal output resolution of cameras, in pixels.
	/// This is the fallback value if no custom resolution is set, and if the camera isn't attached to a window.
	/// </summary>
	public const uint defaultOutputResolutionX = 640;
	/// <summary>
	/// The default vertical output resolution of cameras, in pixels.
	/// This is the fallback value if no custom resolution is set, and if the camera isn't attached to a window.
	/// </summary>
	public const uint defaultOutputResolutionY = 480;

	/// <summary>
	/// The minimum resolution of output render targets, in pixels.
	/// </summary>
	public const uint minOutputResolution = 8;
	/// <summary>
	/// The maximum supported resolution of output render targets, in pixels.
	/// </summary>
	public const uint maxOutputResolution = 8192;

	#endregion
	#region Constants Resources

	/// <summary>
	/// Gets a description for the camera's resource set layout.
	/// </summary>
	/// <remarks>
	/// <b>Layout contents:</b>
	/// <list type="bullet">
	///		<item><see cref="CBGraphics"/>: Constant buffer with engine-wide graphics settings.</item>
	///		<item><see cref="CBScene"/>: Constant buffer with scene-wide graphics settings.</item>
	///		<item><see cref="CBCamera"/>: Constant buffer with graphics settings specific to a camera pass.</item>
	/// </list>
	/// </remarks>
	public static ResourceLayoutDescription ResLayoutCameraDesc => new(
		new ResourceLayoutElementDescription(CBGraphics.resourceName, ResourceKind.UniformBuffer, (ShaderStages)0b111111, ResourceLayoutElementOptions.None),
		new ResourceLayoutElementDescription(CBScene.resourceName, ResourceKind.UniformBuffer, (ShaderStages)0b111111, ResourceLayoutElementOptions.None),
		new ResourceLayoutElementDescription(CBCamera.resourceName, ResourceKind.UniformBuffer, ShaderStages.Fragment, ResourceLayoutElementOptions.None));
		//...

	#endregion
}
