using Veldrid;

namespace FragEngine.Extensions.Veldrid;

/// <summary>
/// Extension methods for the <see cref="RgbaByte"/> struct.
/// </summary>
public static class RgbaByteExt
{
	#region Methods

	/// <summary>
	/// Gets a hexadecimal string representation of this color.
	/// </summary>
	/// <param name="_color">This 32-bit RGBA color.</param>
	/// <returns>A string using the format "FF00FF00".</returns>
	public static string ToHexString(this RgbaByte _color)
	{
		return $"{_color.R:X2}{_color.G:X2}{_color.B:X2}{_color.A:X2}";
	}

	#endregion
}
