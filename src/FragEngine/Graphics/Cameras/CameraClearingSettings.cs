using FragEngine.Interfaces;
using Veldrid;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// Enumeration of different moments where a camera might need to clear render targets.
/// </summary>
[Flags]
public enum CameraClearingFlags : byte
{
	Never		= 0,
	EachFrame	= 1,
	EachPass	= 2,
}

/// <summary>
/// A settings object that describes the desired buffer clearing behaviour of a <see cref="Camera"/>.
/// </summary>
public sealed class CameraClearingSettings : IValidated, IChecksumVersioned
{
	#region Fields

	private ulong checksum = UNINITIALIZED_CHECKSUM;

	private RgbaFloat[] colorValues = [ RgbaFloat.CornflowerBlue ];
	private float depthValue = 1.0f;

	#endregion
	#region Constants

	internal const ulong UNINITIALIZED_CHECKSUM = ulong.MaxValue;

	#endregion
	#region Properties

	/// <summary>
	/// Gets flags indicating when color targets should be cleared by the camera.
	/// </summary>
	public CameraClearingFlags ClearColorTargets { get; init; } = CameraClearingFlags.EachFrame;
	/// <summary>
	/// Gets flags indicating when the depth buffer should be cleared by the camera.
	/// </summary>
	public CameraClearingFlags ClearDepthBuffer { get; init; } = CameraClearingFlags.EachFrame;
	/// <summary>
	/// Gets flags indicating when the stencil buffer should be cleared by the camera.
	/// </summary>
	public CameraClearingFlags ClearStencilBuffer { get; init; } = CameraClearingFlags.EachFrame;

	/// <summary>
	/// Gets a read-only list of colors that each color target should be cleared to.
	/// If this list contains fewer elements than there are color targets, each additional target will be cleared using the last
	/// element in this color list. If there is only one element, all targets will be cleared with that one color.
	/// </summary>
	public IReadOnlyList<RgbaFloat> ColorValues
	{
		get => colorValues;
		init => colorValues = value?.ToArray() ?? [];
	}

	/// <summary>
	/// Gets the depth value that depth buffers should be cleared to. Must be value between 0.0 and 1.0.
	/// </summary>
	public float DepthValue
	{
		get => depthValue;
		init => depthValue = Math.Clamp(value, 0, 1);
	}

	/// <summary>
	/// Gets the stencil value that the stencil buffer should be cleared to.
	/// </summary>
	public byte StencilValue { get; init; } = 0;

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
	/// Gets a set of default placeholder settings that should work in most situations.
	/// </summary>
	public static CameraClearingSettings Default => new()
	{
		ClearColorTargets = CameraClearingFlags.EachFrame,
		ClearDepthBuffer = CameraClearingFlags.EachFrame,
		ClearStencilBuffer = CameraClearingFlags.EachFrame,
		ColorValues = [ RgbaFloat.CornflowerBlue ],
		DepthValue = 1.0f,
		StencilValue = 0x00,
	};

	/// <summary>
	/// Gets a set of settings that will never clear render targets.
	/// </summary>
	public static CameraClearingSettings DontClear => new()
	{
		ClearColorTargets = CameraClearingFlags.Never,
		ClearDepthBuffer = CameraClearingFlags.Never,
		ClearStencilBuffer = CameraClearingFlags.Never,
		ColorValues = [],
	};

	#endregion
	#region Methods

	private ulong CalculateChecksum()
	{
		ulong newChecksum = 0ul;

		// Clearing flags:
		newChecksum |= (ulong)ClearColorTargets << 0;
		newChecksum |= (ulong)ClearDepthBuffer << 3;
		newChecksum |= (ulong)ClearStencilBuffer << 6;

		// Apply color values to checksum:
		if (ClearColorTargets != CameraClearingFlags.Never)
		{
			ulong colorsChecksum = (ulong)ColorValues.Count;
			foreach (RgbaFloat color in ColorValues)
			{
				ulong color10 = ColorTo10bit(color);
				colorsChecksum ^= color10;
			}

			newChecksum |= colorsChecksum << 9;
		}
		// Apply depth/stencil values to checksum:
		if (ClearDepthBuffer != CameraClearingFlags.Never)
		{
			ulong depthChecksum = (ulong)(DepthValue * 1024);
			if (ClearStencilBuffer != CameraClearingFlags.Never)
			{
				depthChecksum ^= (ulong)StencilValue << 7;
			}

			newChecksum |= depthChecksum << 9;
		}

		return newChecksum;


		// Local helper function for packing color into a 10-bpc packed integer:
		static ulong ColorTo10bit(RgbaFloat _color)
		{
			ulong r10 = (ulong)Math.Clamp(_color.R * 512, 0, 1024);
			ulong g10 = (ulong)Math.Clamp(_color.G * 512, 0, 1024);
			ulong b10 = (ulong)Math.Clamp(_color.B * 512, 0, 1024);
			ulong a10 = (ulong)Math.Clamp(_color.A * 512, 0, 1024);
			// ^float values in range 0.0-2.0 are supported, mapped to 0-1024.
			return r10 | (g10 << 10) | (b10 << 20) | (a10 << 30);
		}
	}

	public bool IsValid()
	{
		bool isValid = !(ClearColorTargets != CameraClearingFlags.Never && ColorValues.Count == 0);
		return isValid;
	}

	#endregion
}
