using System.Numerics;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="Vector4"/> struct.
/// </summary>
public static class Vector4Ext
{
	#region Methods

	/// <summary>
	/// Formats the vector into a hexadecimal color representation (ex.: "<c>FF0023B77C</c>").
	/// All axes will be clamped to the 0.0 to 1.0 value range, corresponding to 00..FF in hex.
	/// XYZW axes are mapped to RGBA color channels respectively.
	/// </summary>
	/// <param name="_rgbaVector">This vector encoding a color.</param>
	/// <returns>An 8-digit string containing the hexadecimal color representation of the vector.</returns>
	public static string ToHexColorString(this Vector4 _rgbaVector)
	{
		int r = Math.Clamp((int)(_rgbaVector.X * 255), 0, 255);
		int g = Math.Clamp((int)(_rgbaVector.Y * 255), 0, 255);
		int b = Math.Clamp((int)(_rgbaVector.Z * 255), 0, 255);
		int a = Math.Clamp((int)(_rgbaVector.W * 255), 0, 255);
		return $"{r:X2}{g:X2}{b:X2}{a:X2}";
	}

	#endregion
}
