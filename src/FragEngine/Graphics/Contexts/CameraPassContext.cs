using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Scenes;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

public sealed class CameraPassContext(SceneContext _sceneCtx)
{
	#region Properties

	public SceneContext SceneCtx { get; } = _sceneCtx ?? throw new ArgumentNullException(nameof(_sceneCtx));
	public GraphicsContext GraphicsCtx => SceneCtx.GraphicsCtx;

	public required CommandList CmdList { get; init; }
	public required CBCamera CbCamera { get; init; }
	public required DeviceBuffer BufCbCamera { get; init; }
	public required AABB ViewportFrustumBounds { get; init; }

	#endregion
}
