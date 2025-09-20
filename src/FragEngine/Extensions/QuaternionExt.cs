using System.Numerics;
using System.Runtime.CompilerServices;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="Quaternion"/> struct.
/// </summary>
public static class QuaternionExt
{
	#region Constants

	private const float EPSILON = 0.00001f;

	#endregion
	#region Methods

	/// <summary>
	/// Checks whether this quaternion is roughly equal to another quaternion.
	/// </summary>
	/// <remarks>
	/// This method was added to allow sensible equality checks of quaternions, by allowing some margin of error
	/// to counter floating-point inaccuracies.
	/// </remarks>
	/// <param name="_quaternion">This quaternion.</param>
	/// <param name="_other">Another quaternion to compare against.</param>
	/// <returns>True if they are equal to within rounding errors, false otherwise.</returns>
	public static bool ApproximatelyEqual(this Quaternion _quaternion, Quaternion _other)
	{
		bool isEqual =
			MathF.Abs(_quaternion.X - _other.X) < EPSILON &&
			MathF.Abs(_quaternion.Y - _other.Y) < EPSILON &&
			MathF.Abs(_quaternion.Z - _other.Z) < EPSILON &&
			MathF.Abs(_quaternion.W - _other.W) < EPSILON;
		return isEqual;
	}

	/// <summary>
	/// Normalizes the magnitude of the rotation, to turn it into a unit quaternion.
	/// </summary>
	/// <remarks>
	/// Only unit quaternions may be used as valid rotation quaternions.
	/// </remarks>
	/// <param name="_quaternion">This quaternion.</param>
	/// <returns>The normalized quaternion.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Normalize(this Quaternion _quaternion)
	{
		float rotationMagnitude = _quaternion.Length();
		if (rotationMagnitude > 0 && rotationMagnitude.IsAlmostEqualTo(1))
		{
			_quaternion *= 1.0f / rotationMagnitude;
		}
		return _quaternion;
	}

	/// <summary>
	/// Applies a rotation to a quaternion that represents an object's orientation.
	/// </summary>
	/// <param name="_orientation">A rotation quaternion that describes the orientation of an object or coordinate system.</param>
	/// <param name="_rotationOffset">A rotation to apply to this orientation.</param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Rotate(this Quaternion _orientation, in Quaternion _rotationOffset)
	{
		// q' = p^-1 * (q * p);
		return Quaternion.Conjugate(_rotationOffset) * (_rotationOffset * _orientation);
	}

	#endregion
}
