using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FragEngine.Scenes;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = sizeof(float), Size = byteSize)]
public record struct Pose
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
	/// <param name="_worldMatrix">The transformation matrix.</param>
	public Pose(in Matrix4x4 _worldMatrix)
	{
		if (!Matrix4x4.Decompose(_worldMatrix, out scale, out rotation, out position))
		{
			this = Identity;
		}
	}

	#endregion
	#region Methods

	//TODO

	#endregion
}
