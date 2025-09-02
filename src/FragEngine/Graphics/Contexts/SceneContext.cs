using FragEngine.Graphics.ConstantBuffers;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

public sealed class SceneContext(GraphicsContext _graphicsCtx)
{
	#region Properties

	public GraphicsContext GraphicsCtx { get; } = _graphicsCtx ?? throw new ArgumentNullException(nameof(_graphicsCtx));

	public required CBScene CbScene { get; init; }
	public required DeviceBuffer BufCbScene { get; init; }

	#endregion
}
