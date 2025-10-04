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

	private ulong checksum = UNINITIALIZED_CHECKSUM;

	private uint resolutionX = 8;
	private uint resolutionY = 8;
	private bool hasStencilBuffer = false;

	#endregion
	#region Constants

	internal const ulong UNINITIALIZED_CHECKSUM = 0ul;

	private const uint MIN_RESOLUTION = 8;
	private const uint MAX_RESOLUTION = 8192;

	#endregion
	#region Properties

	/// <summary>
	/// The horizontal output resolution, in pixels.
	/// Must be in the range from 8 to 8192, should be a multiple of 8.
	/// </summary>
	public required uint ResolutionX
	{
		get => resolutionX;
		init => resolutionX = Math.Clamp(value, MIN_RESOLUTION, MAX_RESOLUTION);
	}
	/// <summary>
	/// The vertical output resolution, in pixels.
	/// Must be in the range from 8 to 8192, should be a multiple of 8.
	/// </summary>
	public required uint ResolutionY
	{
		get => resolutionY;
		init => resolutionY = Math.Clamp(value, MIN_RESOLUTION, MAX_RESOLUTION);
	}

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
	/// Gets the aspect ratio of output images. This is a ratio calculated by dividing horizontal through vertical resolution.
	/// </summary>
	public float AspectRatio => (float)ResolutionX / ResolutionY;

	/// <summary>
	/// Gets or calculates a checksum for the current settings.
	/// This value may be used to unambiguously compare if 2 settings objects are identical, and is used for detecting changes.
	/// </summary>
	public ulong Checksum
	{
		get
		{
			if (checksum != UNINITIALIZED_CHECKSUM)
			{
				return checksum;
			}

			checksum = CalculateChecksum();
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

	private ulong CalculateChecksum()
	{
		ulong newChecksum = 0ul;

		newChecksum |= ResolutionX;
		newChecksum |= ResolutionY << 13;

		if (HasColorTarget)
		{
			newChecksum |= (ulong)ColorFormat << 26;
			newChecksum |= 1ul << 42;
		}
		if (HasDepthBuffer)
		{
			newChecksum |= (ulong)DepthFormat << 34;
			newChecksum |= 1ul << 43;
			newChecksum |= (HasStencilBuffer ? 1ul : 0ul) << 44;
		}

		return newChecksum;
	}

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

	/// <summary>
	/// Creates a set of new output settings based on an existing framebuffer.
	/// </summary>
	/// <param name="_srcFramebuffer">A framebuffer to use as reference. May not be null or disposed.</param>
	/// <returns>The new output settings object.</returns>
	/// <exception cref="ArgumentNullException">Source frame buffer may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Source frame buffer may not be disposed.</exception>
	public static CameraOutputSettings CreateFromFramebuffer(in Framebuffer _srcFramebuffer)
	{
		ArgumentNullException.ThrowIfNull(_srcFramebuffer);
		ObjectDisposedException.ThrowIf(_srcFramebuffer.IsDisposed, _srcFramebuffer);

		// Prepare temporary variables:
		Texture? mainTexture = null;

		uint resolutionX = MIN_RESOLUTION;
		uint resolutionY = MIN_RESOLUTION;

		PixelFormat colorFormat = PixelFormat.B8_G8_R8_A8_UNorm;
		PixelFormat depthFormat = PixelFormat.D24_UNorm_S8_UInt;
		TextureSampleCount sampleCount = TextureSampleCount.Count1;

		bool hasColorTargets = false;
		bool hasDepthBuffer = false;
		bool hasStencilBuffer = false;

		// Gather data from textures and buffers:
		if (_srcFramebuffer.ColorTargets is not null && _srcFramebuffer.ColorTargets.Count != 0)
		{
			mainTexture = _srcFramebuffer.ColorTargets[0].Target;
			colorFormat = _srcFramebuffer.ColorTargets[0].Target.Format;
			hasColorTargets = true;
		}
		if (_srcFramebuffer.DepthTarget is not null)
		{
			mainTexture ??= _srcFramebuffer.DepthTarget!.Value.Target;
			depthFormat = _srcFramebuffer.DepthTarget!.Value.Target.Format;
			hasDepthBuffer = true;
			hasStencilBuffer = depthFormat.IsDepthFormat();
		}

		if (mainTexture is not null)
		{
			resolutionX = mainTexture.Width;
			resolutionY = mainTexture.Height;
			sampleCount = mainTexture.SampleCount;
		}

		// Assemble settings object:
		CameraOutputSettings outputSettings = new()
		{
			ResolutionX = resolutionX,
			ResolutionY = resolutionY,
			ColorFormat = colorFormat,
			DepthFormat = depthFormat,
			SampleCount = sampleCount,
			HasColorTarget = hasColorTargets,
			HasDepthBuffer = hasDepthBuffer,
			HasStencilBuffer = hasStencilBuffer,
		};
		return outputSettings;
	}

	#endregion
}
