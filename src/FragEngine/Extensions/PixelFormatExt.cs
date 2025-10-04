using Veldrid;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="PixelFormat"/> enum.
/// </summary>
public static class PixelFormatExt
{
	#region Methods

	/// <summary>
	/// Gets whether this format may be used as a color render target.
	/// </summary>
	/// <param name="pixelFormat">This pixel format.</param>
	/// <returns>True if the format is supported for color targets, false otherwise.</returns>
	public static bool IsColorTargetFormat(this PixelFormat pixelFormat)
	{
		bool isColorTarget =
			!IsStencilFormat(pixelFormat) &&
			!IsCompressed(pixelFormat);
		return isColorTarget;
	}

	/// <summary>
	/// Gets whether this format may be used for depth buffers.
	/// </summary>
	/// <param name="pixelFormat">This pixel format.</param>
	/// <returns>True if the format is supported for depth buffers, false otherwise.</returns>
	public static bool IsDepthFormat(this PixelFormat pixelFormat)
	{
		bool isDeph = pixelFormat switch
		{
			PixelFormat.R16_UNorm or
			PixelFormat.R32_Float or
			PixelFormat.D24_UNorm_S8_UInt or
			PixelFormat.D32_Float_S8_UInt => true,
			_ => false,
		};
		return isDeph;
	}

	/// <summary>
	/// Gets whether this format may be used for depth+stencil buffers.
	/// </summary>
	/// <param name="pixelFormat">This pixel format.</param>
	/// <returns>True if the format is supported for combined depth+stencil buffers, false otherwise.</returns>
	public static bool IsStencilFormat(this PixelFormat pixelFormat)
	{
		bool isDeph = pixelFormat switch
		{
			PixelFormat.D24_UNorm_S8_UInt or
			PixelFormat.D32_Float_S8_UInt => true,
			_ => false,
		};
		return isDeph;
	}

	/// <summary>
	/// Gets whether this is a compressed pixel format.
	/// </summary>
	/// <param name="pixelFormat">This pixel format.</param>
	/// <returns>True if the format's data is block-compressed, false if it's uncompressed.</returns>
	public static bool IsCompressed(this PixelFormat pixelFormat)
	{
		bool isCompressed = pixelFormat switch
		{
			PixelFormat.BC1_Rgb_UNorm or
			PixelFormat.BC1_Rgba_UNorm or
			PixelFormat.BC1_Rgb_UNorm_SRgb or
			PixelFormat.BC1_Rgba_UNorm_SRgb or
			PixelFormat.BC2_UNorm or
			PixelFormat.BC2_UNorm_SRgb or
			PixelFormat.BC3_UNorm or
			PixelFormat.BC3_UNorm_SRgb or
			PixelFormat.BC4_UNorm or
			PixelFormat.BC4_SNorm or
			PixelFormat.BC5_UNorm or
			PixelFormat.BC5_SNorm or
			PixelFormat.BC7_UNorm or
			PixelFormat.BC7_UNorm_SRgb or
			PixelFormat.ETC2_R8_G8_B8_UNorm or
			PixelFormat.ETC2_R8_G8_B8_A1_UNorm or
			PixelFormat.ETC2_R8_G8_B8_A8_UNorm => true,
			_ => false,
		};
		return isCompressed;
	}

	#endregion
}
