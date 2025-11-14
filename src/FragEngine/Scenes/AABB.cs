using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FragEngine.Scenes;

/// <summary>
/// Structure describing an axis-aligned bounding box (AABB).
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = byteSize)]
public readonly record struct AABB
{
	#region Types

	/// <summary>
	/// Buffer structure containing the 8 corner points of an <see cref="AABB"/>.
	/// </summary>
	[InlineArray(8)]
	public unsafe struct CornerPoints
	{
		private Vector3 element;
	}

	#endregion
	#region Constants

	/// <summary>
	/// The size of this struct, in bytes.
	/// </summary>
	public const int byteSize = 6 * sizeof(float);

	#endregion
	#region Properties

	/// <summary>
	/// A position representing the lower extent of the bounding box.
	/// </summary>
	public Vector3 Minimum { get; init; }
	/// <summary>
	/// A position representing the upper extent of the bounding box.
	/// </summary>
	public Vector3 Maximum { get; init; }

	/// <summary>
	/// Gets the center position of the bounding box.
	/// </summary>
	public Vector3 Center => (Minimum + Maximum) * 0.5f;

	/// <summary>
	/// Gets the dimensions of the enclosed volume.
	/// </summary>
	public Vector3 Size => Maximum - Minimum;

	/// <summary>
	/// Gets the magnitude of the enclosed volume.
	/// </summary>
	public float Volume => (Maximum.X - Minimum.X) * (Maximum.Y - Minimum.Y) * (Maximum.Z - Minimum.Z);

	/// <summary>
	/// Gets a bounding box centered on the coorindate origin with zero size.
	/// </summary>
	public static AABB Zero => new()
	{
		Minimum = Vector3.Zero,
		Maximum = Vector3.Zero,
	};

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new bounding box from lower and upper extents.
	/// </summary>
	/// <param name="_minimum">The lower extents of the bounding box.</param>
	/// <param name="_maximum">The upper extents of the bounding box.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public AABB(Vector3 _minimum, Vector3 _maximum)
	{
		Minimum = Vector3.Min(_minimum, _maximum);
		Maximum = Vector3.Max(_minimum, _maximum);
	}

	/// <summary>
	/// Creates a new bounding box from a set of enclosed positions.
	/// </summary>
	/// <param name="_points">An array of position coordinates that are enclosed by the bounding box.
	/// May not be null. If the array is empty, the AABB's extents will be set to zero.</param>
	/// <exception cref="ArgumentNullException">Points may not be null.</exception>
	public AABB(params Vector3[] _points)
	{
		ArgumentNullException.ThrowIfNull(_points);

		if (_points.Length == 0)
		{
			Minimum = Vector3.Zero;
			Maximum = Vector3.Zero;
			return;
		}

		Vector3 min = _points[0];
		Vector3 max = _points[0];
		for (int i = 1; i < _points.Length; i++)
		{
			min = Vector3.Min(_points[i], min);
			max = Vector3.Max(_points[i], max);
		}

		Minimum = min;
		Maximum = max;
	}

	/// <summary>
	/// Creates a new bounding box from a set of corner points.
	/// </summary>
	/// <param name="_points">A fixed-size buffer of position coordinates that represent the 8 corners of an AABB.</param>
	public AABB(in CornerPoints _points)
	{
		Vector3 min = _points[0];
		Vector3 max = _points[0];
		for (int i = 1; i < 8; i++)
		{
			min = Vector3.Min(_points[i], min);
			max = Vector3.Max(_points[i], max);
		}

		Minimum = min;
		Maximum = max;
	}

	/// <summary>
	/// Creates a new bounding box that encloses two other AABBs.
	/// </summary>
	/// <param name="_boundsA">The first bounding box.</param>
	/// <param name="_boundsB">The second bounding box.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public AABB(in AABB _boundsA, in AABB _boundsB)
	{
		Minimum = Vector3.Min(_boundsA.Minimum, _boundsB.Minimum);
		Maximum = Vector3.Max(_boundsA.Maximum, _boundsB.Maximum);
	}

	#endregion
	#region Methods

	/// <summary>
	/// Checks whether a position is enclosed by this bounding box.
	/// </summary>
	/// <param name="_point">A position coordinate.</param>
	/// <returns>True if the point lies within the AABB, false otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Contains(Vector3 _point)
	{
		return
			_point.X >= Minimum.X &&
			_point.Y >= Minimum.Y &&
			_point.Z >= Minimum.Z &&
			_point.X <= Maximum.X &&
			_point.Y <= Maximum.Y &&
			_point.Z <= Maximum.Z;
	}

	/// <summary>
	/// Checks whether this bounding box overlaps with another bounding box.
	/// </summary>
	/// <param name="_other">The other bounding box.</param>
	/// <returns>True if the two bounding boxes overlap, false otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Overlaps(in AABB _other)
	{
		return
			!(_other.Minimum.X > Maximum.X || _other.Maximum.X < Minimum.X) &&
			!(_other.Minimum.Y > Maximum.Y || _other.Maximum.Y < Minimum.Y) &&
			!(_other.Minimum.Z > Maximum.Z || _other.Maximum.Z < Minimum.Z);
	}

	/// <summary>
	/// Gets all 8 corner points of the bounding box.
	/// </summary>
	/// <returns>A buffer structure containing all 8 corner points.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CornerPoints GetCornerPoints()
	{
		CornerPoints points = new();
		points[0] = Minimum;
		points[1] = new(Maximum.X, Minimum.Y, Maximum.Z);
		points[2] = new(Minimum.X, Maximum.Y, Minimum.Z);
		points[3] = new(Minimum.X, Minimum.Y, Maximum.Z);
		points[4] = new(Minimum.X, Maximum.Y, Maximum.Z);
		points[5] = new(Maximum.X, Minimum.Y, Maximum.Z);
		points[6] = new(Maximum.X, Maximum.Y, Minimum.Z);
		points[7] = Minimum;
		return points;
	}

	/// <summary>
	/// Transforms the bounding box from local space to a parent space, as described by a pose.
	/// </summary>
	///	<remarks>
	///	NOTE: This will always make the AABB larger if there is a non-zero rotation or a scale of more than 1.
	///	Do not compound transforation operations on a bounding box! Instead, recalculate the final transformed
	///	AABB directly from pre-compounded poses and transformations. This operations may be slow and heavy as
	///	it involves a lot of math with vectors and quaternions.
	///	</remarks>
	/// <param name="_pose">A pose that describes the volume's transformation in space.</param>
	/// <returns>The transformed bounding box volume.</returns>
	public readonly AABB Transform(in Pose _pose)
	{
		CornerPoints points = GetCornerPoints();
		for (int i = 0; i < 8; ++i)
		{
			points[i] = _pose.Transform(points[i]);
		}
		return new AABB(points);
	}

	public override string ToString()
	{
		return $"Minimum: {Minimum}, Maximum: {Maximum}, Size: {Size}, Volume: {Volume:0.##}";
	}

	#endregion
}
