using FragEngine.Extensions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FragEngine.Scenes;

/// <summary>
/// A Pose in 3D space. A pose is defined by a position, rotation, and scale, each relative to a parent space.
/// </summary>
/// <remarks>
/// Note that while a pose instance is always defined in some parent coordinate space, it does not carry any information about this parent
/// space. Management of nested coordinate spaces must be handled by consuming types.
/// <para/>
/// This implementation assumes a left-handed (i.e. DirectX-style) coordinate space. The Y-axis points up, and the Z-axis points forward.
/// If you're working with poses and coordinate systems of different handedness, consider adding a suffix to your variable names, to clearly
/// differentiate which handedness applies for each value. For example: LH for left-handed, and RH for right-handed.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = sizeof(float), Size = byteSize)]
public record struct Pose : IEquatable<Pose>
{
	#region Fields

	/// <summary>
	/// Position or translation in parent space.
	/// </summary>
	public Vector3 position;
	/// <summary>
	/// Rotation relative to parent space.
	/// </summary>
	public Quaternion rotation;
	/// <summary>
	/// Scale factors in local space.
	/// </summary>
	public Vector3 scale;

	#endregion
	#region Constants

	/// <summary>
	/// The size in bytes of this structure.
	/// </summary>
	public const int byteSize = (3 + 4 + 3) * sizeof(float); // = 40 bytes

	private const float EPSILON = 0.0001f;

	#endregion
	#region Properties

	/// <summary>
	/// Gets or sets the world transformation matrix of this pose.
	/// </summary>
	public Matrix4x4 WorldMatrix
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get => Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
		set => this = new Pose(in value);
	}

	/// <summary>
	/// Gets or sets the world transformation matrix of this pose, with scale set to 100%.
	/// </summary>
	public Matrix4x4 UnscaledMatrix
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get => Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
		set
		{
			this = new Pose(in value);
			scale = Vector3.One;
		}
	}

	/// <summary>
	/// Gets or sets the world transformation matrix of this pose, with no position/translation.
	/// </summary>
	public Matrix4x4 DirectionMatrix
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly get => Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation);
		set
		{
			this = new Pose(in value);
			position = Vector3.Zero;
		}
	}

	/// <summary>
	/// Gets the "right" direction, along the X-axis in local space.
	/// </summary>
	public readonly Vector3 Right
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Vector3.Transform(Vector3.UnitX, rotation);
	}

	/// <summary>
	/// Gets the "up" direction, along the Y-axis in local space.
	/// </summary>
	public readonly Vector3 Up
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Vector3.Transform(Vector3.UnitY, rotation);
	}

	/// <summary>
	/// Gets the "forward" direction, along the Z-axis in local space.
	/// </summary>
	public readonly Vector3 Forward
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Vector3.Transform(Vector3.UnitZ, rotation);
	}

	/// <summary>
	/// Gets a pose that is equivalent to an identity matrix. Position is zero, no rotation, and scale is 100%.
	/// </summary>
	public static Pose Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a pose.
	/// </summary>
	public Pose()
	{
		position = Vector3.Zero;
		rotation = Quaternion.Identity;
		scale = Vector3.One;
	}

	/// <summary>
	/// Creates a pose, with a position.
	/// </summary>
	/// <param name="_position">The position or translation coordinate.</param>
	public Pose(Vector3 _position)
	{
		position = _position;
		rotation = Quaternion.Identity;
		scale = Vector3.One;
	}

	/// <summary>
	/// Creates a pose, with a position and rotation.
	/// </summary>
	/// <param name="_position">The position or translation coordinate.</param>
	/// <param name="_rotation">The orientation of the local coordinate space.</param>
	public Pose(Vector3 _position, Quaternion _rotation)
	{
		position = _position;
		rotation = _rotation;
		scale = Vector3.One;
	}

	/// <summary>
	/// Creates a pose, with a position, rotation, and scale.
	/// </summary>
	/// <param name="_position">The position or translation coordinate.</param>
	/// <param name="_rotation">The orientation of the local coordinate space.</param>
	/// <param name="_scale">The scale factors of the local coordinate space.</param>
	public Pose(Vector3 _position, Quaternion _rotation, Vector3 _scale)
	{
		position = _position;
		rotation = _rotation;
		scale = _scale;
	}

	/// <summary>
	/// Creates a pose from a 3D transformation or world matrix.
	/// </summary>
	/// <param name="_mtxWorld">The transformation matrix.</param>
	public Pose(in Matrix4x4 _mtxWorld)
	{
		if (!Matrix4x4.Decompose(_mtxWorld, out scale, out rotation, out position))
		{
			this = Identity;
		}
	}

	#endregion
	#region Methods General

	/// <summary>
	/// Decomposes the pose into its basic transformation components.
	/// </summary>
	/// <param name="_outPosition">Outputs the position or translation.</param>
	/// <param name="_outRotation">Outputs the orientation or rotation.</param>
	/// <param name="_outScale">Outputs the scaling factors.</param>
	public readonly void Deconstruct(out Vector3 _outPosition, out Quaternion _outRotation, out Vector3 _outScale)
	{
		_outPosition = position;
		_outRotation = rotation;
		_outScale = scale;
	}

	public static bool operator ==(in Pose _a, in Pose _b) => _a.position == _b.position && _a.rotation == _b.rotation && _a.scale == _b.scale;
	public static bool operator !=(in Pose _a, in Pose _b) => _a.position != _b.position || _a.rotation != _b.rotation || _a.scale != _b.scale;

	public readonly override string ToString()
		=> $"Position: ({position.X:0.##}; {position.Y:0.##}; {position.Z:0.##}), Rotation: ({rotation.X:0.##}; {rotation.Y:0.##}; {rotation.Z:0.##}; {rotation.W:0.##}), Scale: ({scale.X:0.##}; {scale.Y:0.##}; {scale.Z:0.##})";

	#endregion
	#region Methods Transformations

	/// <summary>
	/// Normalizes the magnitude of the rotation, to ensure it is a unit quaternion.
	/// </summary>
	public void Normalize() => rotation = rotation.Normalize();

	/// <summary>
	/// Moves the pose's position.
	/// </summary>
	/// <param name="_offset">The translation distance along each axis.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Translate(Vector3 _offset) => position += _offset;

	/// <summary>
	/// Moves the pose's position.
	/// </summary>
	/// <param name="_x">The translation distance along the X-axis, i.e. to the right..</param>
	/// <param name="_y">The translation distance along the Y-axis, i.e. upwards.</param>
	/// <param name="_z">The translation distance along the Z-axis, i.e. forwards.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Translate(float _x, float _y, float _z) => position += new Vector3(_x, _y, _z);

	/// <summary>
	/// Applies a rotation to the pose.
	/// </summary>
	/// <param name="_rotationOffset">The rotation offset, must be a unit quaternion.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Rotate(Quaternion _rotationOffset) => rotation = rotation.Rotate(in _rotationOffset);

	/// <summary>
	/// Changes the scale of the pose.
	/// </summary>
	/// <param name="_scaleFactors">Component-wise scale factors, to be multiplied with <see cref="scale"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Scale(Vector3 _scaleFactors) => scale *= _scaleFactors;

	/// <summary>
	/// Changes the scale of the pose.
	/// </summary>
	/// <param name="_scaleFactor">A scale factor, to be multiplied with <see cref="scale"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Scale(float _scaleFactor) => scale *= _scaleFactor;

	#endregion
	#region Methods Hierarchy

	/// <summary>
	/// Transforms this pose into the same coordinate system as its parent space.
	/// </summary>
	///	<remarks>
	///	Example: Use this to transform a pose from local space to world space.
	///	</remarks>
	/// <param name="_parentSpacePose">A pose that describes the parent space's coordinate system.</param>
	/// <returns>The transformed pose.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Pose TransformToParentSpace(in Pose _parentSpacePose)
	{
		Pose result = new(
			_parentSpacePose.position + Vector3.Transform(position * _parentSpacePose.scale, _parentSpacePose.rotation),
			Quaternion.Concatenate(_parentSpacePose.rotation, rotation),
			scale * _parentSpacePose.scale);
		return result;
	}

	/// <summary>
	/// Transforms this pose to be in the coordinate system of a parent space.
	/// </summary>
	///	<remarks>
	///	Example: Use this to transform a pose from world space to local space.
	///	</remarks>
	/// <param name="_parentSpacePose">A pose that describes the target space's coordinate system.</param>
	/// <returns>The transformed pose.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Pose TransformToLocalSpace(in Pose _parentSpacePose)
	{
		Quaternion invParentRot = Quaternion.Inverse(_parentSpacePose.rotation);
		Vector3 invParentScale = Vector3.One / Vector3.Max(_parentSpacePose.scale, new Vector3(EPSILON));
		Pose result = new(
			Vector3.Transform(position - _parentSpacePose.position, invParentRot) * invParentScale,
			invParentRot * rotation,
			scale * invParentScale);
		return result;
	}

	#endregion
}
