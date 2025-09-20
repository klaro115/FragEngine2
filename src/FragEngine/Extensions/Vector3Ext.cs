using System.Numerics;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="Vector3"/> struct.
/// </summary>
public static class Vector3Ext
{
	#region Constants

	private const float EPSILON = 0.00001f;

	#endregion
	#region Methods

	/// <summary>
	/// Checks whether this vector is roughly equal to another vector.
	/// </summary>
	/// <remarks>
	/// This method was added to allow sensible equality checks of vectors, by allowing some margin of error
	/// to counter floating-point inaccuracies.
	/// </remarks>
	/// <param name="_vector">This vector.</param>
	/// <param name="_other">Another vector to compare against.</param>
	/// <returns>True if they are equal to within rounding errors, false otherwise.</returns>
	public static bool ApproximatelyEqual(this Vector3 _vector, Vector3 _other)
	{
		bool isEqual =
			MathF.Abs(_vector.X - _other.X) < EPSILON &&
			MathF.Abs(_vector.Y - _other.Y) < EPSILON &&
			MathF.Abs(_vector.Z - _other.Z) < EPSILON;
		return isEqual;
	}

	#endregion
}
