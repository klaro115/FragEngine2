using System.Numerics;
using Veldrid;

namespace FragEngine.EngineCore.Input.Axes;

/// <summary>
/// A singular input axis. This can be either a joystick stick axis, or a mapped set of 2 keys.
/// The current axis value will be in the range [-1;1], and can be read from <see cref="CurrentValue"/>.
/// </summary>
public abstract class InputAxis(string _name)
{
	#region Fields

	/// <summary>
	/// The name of this input axis. This serves as a unique identifier.
	/// </summary>
	public readonly string name = _name ?? throw new ArgumentNullException(nameof(_name));

	#endregion
	#region Properties

	/// <summary>
	/// The current value of this input axis, in a range from -1 to +1.
	/// </summary>
	public float CurrentValue { get; protected set; } = 0.0f;
	/// <summary>
	/// The previous frame's value of this input axis, in a range from -1 to +1.
	/// </summary>
	public float PreviousValue { get; protected set; } = 0.0f;

	/// <summary>
	/// The size of the dead-zone for this axis. Input values with a magnitude lower than this
	/// threashold will be treated as 0.
	/// </summary>
	public float DeadZone { get; protected set; } = 0.0f;
	/// <summary>
	/// The type of this input axis.
	/// </summary>
	public abstract InputAxisType Type { get; }
	/// <summary>
	/// Gets or sets whether input values are discrete. If false, they are either analog or interpolated.
	/// Setting this may change the <see cref="Type"/> of the axis; not all values are supported,
	/// </summary>
	public abstract bool ValuesAreDiscrete { get; set; }

	#endregion
	#region Methods

	/// <summary>
	/// Try to assign a new type to this axis. This will change the way input values are processed.
	/// Note that only some input types may be supported, depending on the underlying input device.
	/// </summary>
	/// <param name="_newType">The new input type.</param>
	/// <returns>True if the input type is supported and was assigned, false otherwise.</returns>
	public abstract bool SetInputType(InputAxisType _newType);

	/// <summary>
	/// Updates the value of the axis.
	/// </summary>
	/// <param name="_snapshot">An input snapshot.</param>
	/// <param name="_deltatime">The duration of the last frame, in seconds.</param>
	internal abstract void Update(InputSnapshot? _snapshot, float _deltatime);

	#endregion
	#region Methods Static

	/// <summary>
	/// Gets a normalized 2D direction from 2 input axes.
	/// </summary>
	/// <param name="_axisX">An input axis mapping to the X-component of the direction.</param>
	/// <param name="_axisY">An input axis mapping to the Y-component of the direction.</param>
	/// <returns>A direction with a length of 1. The length may be 0 if current value of all axes are zero.</returns>
	public static Vector2 GetNormalizedInputDirection(InputAxis _axisX, InputAxis _axisY)
	{
		Vector2 rawDirection = new(_axisX.CurrentValue, _axisY.CurrentValue);
		return Vector2.Normalize(rawDirection);
	}

	/// <summary>
	/// Gets a normalized 3D direction from 3 input axes.
	/// </summary>
	/// <param name="_axisX">An input axis mapping to the X-component of the direction.</param>
	/// <param name="_axisY">An input axis mapping to the Y-component of the direction.</param>
	/// <param name="_axisZ">An input axis mapping to the Z-component of the direction.</param>
	/// <returns>A direction with a length of 1. The length may be 0 if current value of all axes are zero.</returns>
	public static Vector3 GetNormalizedInputDirection(InputAxis _axisX, InputAxis _axisY, InputAxis _axisZ)
	{
		Vector3 rawDirection = new(_axisX.CurrentValue, _axisY.CurrentValue, _axisZ.CurrentValue);
		return Vector3.Normalize(rawDirection);
	}

	#endregion
}
