namespace FragEngine.EngineCore.Input.Keys;

/// <summary>
/// Enumeration of the different change events that can take place in a key between 2 subsequent frames.
/// </summary>
public enum InputKeyEventType
{
	/// <summary>
	/// No change in key state, no event.
	/// </summary>
	Unchanged,
	/// <summary>
	/// The key is now pressed.
	/// </summary>
	Clicked,
	/// <summary>
	/// The key was just released.
	/// </summary>
	Released,
}
