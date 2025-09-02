using FragEngine.Graphics.ConstantBuffers;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

public sealed class GraphicsContext
{
	#region Properties

	public required GraphicsService Graphics { get; init; }

	public required CBGraphics CbGraphics { get; init; }
	public required DeviceBuffer BufCbGraphics { get; init; }

	#endregion
}
