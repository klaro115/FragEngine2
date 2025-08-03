namespace FragEngine.EngineCore.Input.Axes;

/// <summary>
/// Enumeration of different input axis types. Each type has slightly different behaviour when values change.
/// </summary>
public enum InputAxisType
{
	/// <summary>
	/// Input states are discrete. When a button or input direction is given, the value of the axis will
	/// immediately switch to the appropriate maximum value. If there is no input, the value snaps to 0.
	/// </summary>
	Discrete,
	/// <summary>
	/// Input signals are discrete, i.e. button presses, but axis values are interpolated from the current
	/// to the destination value. This results in smooth transitions between axis values, but with higher
	/// input latency.
	/// </summary>
	Interpolated,
	/// <summary>
	/// Input signals are analog, and the axis value is provided as-is. This is ideal for non-discrete
	/// controls using a gamepad.
	/// </summary>
	Analog,
}
