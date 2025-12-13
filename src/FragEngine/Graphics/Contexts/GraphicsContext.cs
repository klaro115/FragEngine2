using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Interfaces;
using Veldrid;

namespace FragEngine.Graphics.Contexts;

/// <summary>
/// A context object containing graphics infos and overarching resources for a single frame.
/// </summary>
/// <remarks>
/// WARNING: This is a transient instance that is only valid for one frame! Do not attempt
/// to keep any references to this object or the resources referenced therein, as they may
/// be subject to change in-between frames.
/// </remarks>
public sealed class GraphicsContext : IValidated, IChecksumVersioned
{
	#region Properties General

	/// <summary>
	/// The engine's graphics service.
	/// </summary>
	public required GraphicsService Graphics { get; init; }
	public required ulong Checksum { get; init; }

	#endregion
	#region Properties Resources

	/// <summary>
	/// Contents of the graphics constant buffer 'CBGraphics'.
	/// </summary>
	public required CBGraphics CbGraphics { get; init; }
	/// <summary>
	/// A constant buffer containing engine-wide graphics-related information and settings.
	/// </summary>
	public required DeviceBuffer BufCbGraphics { get; init; }

	/// <summary>
	/// The camera's resource layout.
	/// </summary>
	public required ResourceLayout ResLayoutCamera { get; init; }

	#endregion
	#region Methods

	public bool IsValid()
	{
		bool isValid =
			Graphics is not null &&
			BufCbGraphics is not null &&
			!Graphics.IsDisposed &&
			!BufCbGraphics.IsDisposed;
		return isValid;
	}

	#endregion
}
