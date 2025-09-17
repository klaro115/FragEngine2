using FragEngine.Extensions;
using FragEngine.Interfaces;
using Veldrid;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// A settings object that describes the desired output format of a <see cref="Camera"/>.
/// </summary>
public sealed class CameraOutputSettings : IValidated, IChecksumVersioned
{
	#region Fields

	private ulong checksum = 0ul;
	private bool hasStencilBuffer = false;

	#endregion
	#region Properties

	/// <summary>
	/// The horizontal output resolution, in pixels.
	/// Must be in the range from 8 to 8192, should be a multiple of 8.
	/// </summary>
	public required uint ResolutionX { get; init; }
	/// <summary>
	/// The vertical output resolution, in pixels.
	/// Must be in the range from 8 to 8192, should be a multiple of 8.
	/// </summary>
	public required uint ResolutionY { get; init; }

	/// <summary>
	/// The pixel format of the main color render target.
	/// </summary>
	public PixelFormat ColorFormat { get; init; } = PixelFormat.B8_G8_R8_A8_UNorm;
	/// <summary>
	/// The pixel format of the depth buffer.
	/// </summary>
	public PixelFormat DepthFormat { get; init; } = PixelFormat.D24_UNorm_S8_UInt;
	/// <summary>
	/// The number of samples per pixel, MSAA only.
	/// </summary>
	public TextureSampleCount SampleCount { get; init; } = TextureSampleCount.Count1;

	/// <summary>
	/// Whether a color target is needed. If true, '<see cref="ColorFormat"/>' must be a valid color target format.
	/// </summary>
	public bool HasColorTarget { get; init; } = true;
	/// <summary>
	/// Whether a depth buffer is needed. If true, '<see cref="DepthFormat"/>' must be a valid depth format.
	/// </summary>
	public bool HasDepthBuffer { get; init; } = true;
	/// <summary>
	/// Whether a stencil buffer is needed. If true, '<see cref="DepthFormat"/>' must be a valid depth+stencil format.
	/// </summary>
	public bool HasStencilBuffer
	{
		get => HasDepthBuffer && hasStencilBuffer;
		init => hasStencilBuffer = value;
	}

	/// <summary>
	/// Gets or calculates a checksum for the current settings.
	/// This value may be used to unambiguously compare if 2 settings objects are identical, and is used for detecting changes.
	/// </summary>
	public ulong Checksum
	{
		get
		{
			if (checksum != 0)
			{
				return checksum;
			}

			checksum |= ResolutionX;
			checksum |= ResolutionY << 13;
			if (HasColorTarget)
			{
				checksum |= (ulong)ColorFormat << 26;
				checksum |= 1ul << 42;
			}
			if (HasDepthBuffer)
			{
				checksum |= (ulong)DepthFormat << 34;
				checksum |= 1ul << 43;
				checksum |= (HasStencilBuffer ? 1ul : 0ul) << 44;
			}
			return checksum;
		}
	}

	/// <summary>
	/// Gets a set of default placeholder settings that should work on any system.
	/// </summary>
	public static CameraOutputSettings Default => new()
	{
		ResolutionX = 640,
		ResolutionY = 480,
		HasColorTarget = true,
		HasDepthBuffer = true,
		HasStencilBuffer = false,
	};

	#endregion
	#region Methods

	/// <summary>
	/// Checks whether these output settings are valid and make sense.
	/// </summary>
	/// <returns>True if valid, false otherwise.</returns>
	public bool IsValid()
	{
		bool isValid =
			ResolutionX >= 8 &&
			ResolutionY >= 8 &&
			(HasColorTarget || HasDepthBuffer) &&
			(!HasColorTarget || ColorFormat.IsColorTargetFormat()) &&
			(!HasDepthBuffer || DepthFormat.IsDepthFormat()) &&
			(!HasStencilBuffer || DepthFormat.IsStencilFormat());
		return isValid;
	}

	public override bool Equals(object? obj) => obj is CameraOutputSettings other && other.Checksum == Checksum;
	public override int GetHashCode() => base.GetHashCode();

	public override string ToString()
		=> $"Resolution: {ResolutionX}x{ResolutionY}p, Color: {(HasColorTarget ? ColorFormat.ToString() : "None")}, Depth: {(HasDepthBuffer ? DepthFormat.ToString() : "None")}, Stencil: {HasStencilBuffer}";

	/// <summary>
	/// Creates the most basic output description for these settings, assuming only one color target is needed.
	/// </summary>
	/// <returns>An output description.</returns>
	public OutputDescription CreateBasicOutputDescription()
	{
		OutputDescription desc = new(
			HasDepthBuffer ? new OutputAttachmentDescription(DepthFormat) : null,
			HasColorTarget ? [new OutputAttachmentDescription(ColorFormat)] : [],
			SampleCount);
		return desc;
	}

	#endregion
}
