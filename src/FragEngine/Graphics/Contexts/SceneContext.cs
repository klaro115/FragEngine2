using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Interfaces;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

public sealed class SceneContext(GraphicsContext _graphicsCtx) : IValidated
{
	#region Properties

	public GraphicsContext GraphicsCtx { get; } = _graphicsCtx ?? throw new ArgumentNullException(nameof(_graphicsCtx));

	public required CBScene CbScene { get; init; }
	public required DeviceBuffer BufCbScene { get; init; }

	#endregion
	#region Methods

	public bool IsValid()
	{
		bool isValid =
			GraphicsCtx is not null &&
			BufCbScene is not null &&
			!BufCbScene.IsDisposed;
		return isValid;
	}

	#endregion
}
