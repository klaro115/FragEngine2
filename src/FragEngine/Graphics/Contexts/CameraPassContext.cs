using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Interfaces;
using FragEngine.Scenes;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

/// <summary>
/// A context object containing graphics infos and overarching resources for a single camera pass.
/// </summary>
/// <remarks>
/// WARNING: This is a transient instance that is only valid for one camera pass! Do not attempt
/// to keep any references to this object or the resources referenced therein, as they may be
/// subject to change in-between passes.
/// </remarks>
/// <param name="_sceneCtx"></param>
/// <exception cref="ArgumentNullException">Scene context may not be null.</exception>
public sealed class CameraPassContext(SceneContext _sceneCtx) : IChecksumVersioned
{
	#region Properties General

	/// <summary>
	/// Gets the scene context associated with the current frame and camera.
	/// </summary>
	public SceneContext SceneCtx { get; } = _sceneCtx ?? throw new ArgumentNullException(nameof(_sceneCtx));
	/// <summary>
	/// Gets the graphics context associated with the current frame.
	/// </summary>
	public GraphicsContext GraphicsCtx => SceneCtx.GraphicsCtx;
	public required ulong Checksum { get; init; }

	#endregion
	#region Properties Resources

	/// <summary>
	/// The command list used to issue draw calls for this camera pass.
	/// </summary>
	public required CommandList CmdList { get; init; }

	/// <summary>
	/// A resource set with bindable graphics resources that are shared across all rendered objects.
	/// </summary>
	public required ResourceSet ResSetCamera { get; init; }

	/// <summary>
	/// Contents of the camera constant buffer 'CBCamera'.
	/// </summary>
	public required CBCamera CbCamera { get; init; }
	/// <summary>
	/// A constant buffer containing camera-related information and settings for this pass.
	/// </summary>
	public required DeviceBuffer BufCbCamera { get; init; }

	#endregion
	#region Properties Parameters

	/// <summary>
	/// A bounding box volume enclosing the entire viewport frustum. This may be used to filter out renderers not visible to this camera.
	/// </summary>
	public required AABB ViewportFrustumBounds { get; init; }

	#endregion
}
