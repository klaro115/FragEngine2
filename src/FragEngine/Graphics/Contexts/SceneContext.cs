using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Interfaces;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

/// <summary>
/// 
/// </summary>
/// <param name="_graphicsCtx"></param>
/// <exception cref="ArgumentNullException">Scene context may not be null.</exception>
public sealed class SceneContext(GraphicsContext _graphicsCtx) : IValidated, IChecksumVersioned
{
	#region Properties General

	/// <summary>
	/// Gets the graphics context associated with the current frame.
	/// </summary>
	public GraphicsContext GraphicsCtx { get; } = _graphicsCtx ?? throw new ArgumentNullException(nameof(_graphicsCtx));
	public required ulong Checksum { get; init; }

	#endregion
	#region Properties Resources

	/// <summary>
	/// Contents of the scene constant buffer 'CBScene'.
	/// </summary>
	public required CBScene CbScene { get; init; }
	/// <summary>
	/// A constant buffer containing scene-wide information and settings for this frame.
	/// </summary>
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
