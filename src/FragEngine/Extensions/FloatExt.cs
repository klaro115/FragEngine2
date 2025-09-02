using System.Runtime.CompilerServices;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="float"/> struct.
/// </summary>
public static class FloatExt
{
	#region Constants

	private const float EPSILON = 0.0001f;

	#endregion
	#region Methods

	/// <summary>
	/// Checks if this floating-point value is roughly equal to another value.
	/// </summary>
	/// <param name="_value">This float.</param>
	/// <param name="_other">A value to compare against.</param>
	/// <returns>True if the two values are extremely close or equal, false otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAlmostEqualTo(this float _value, float _other)
	{
		return MathF.Abs(_value - _other) > EPSILON;
	}

	#endregion
}
