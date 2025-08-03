using FragEngine.EngineCore.Input.Keys;
using Veldrid;

namespace FragEngine.EngineCore.Input;

public sealed class InputKeyState(Key _key)
{
	#region Fields

	public readonly Key key = _key;

	#endregion
	#region Properties

	/// <summary>
	/// Gets whether the key is currently pressed.
	/// </summary>
	public bool IsPressed { get; private set; } = false;
	/// <summary>
	/// Gets whether the key was pressed during the last frame.
	/// </summary>
	public bool WasPressed { get; private set; } = false;
	/// <summary>
	/// Gets the type of change that happened since last frame.
	/// </summary>
	public InputKeyEventType EventType { get; private set; } = InputKeyEventType.None;

	internal uint VersionIdx { get; private set; } = 0u;

	#endregion
	#region Methods

	/// <summary>
	/// Updates the state of this key.
	/// </summary>
	/// <param name="_isPressed">Whether the key is currently pressed down.</param>
	/// <param name="_versionIdx">Version of the keyboard input state.</param>
	/// <returns>True if the key's state changed, false if it remained the same.</returns>
	internal bool UpdateState(bool _isPressed, uint _versionIdx)
	{
		WasPressed = IsPressed;
		IsPressed = _isPressed;
		VersionIdx = _versionIdx;

		if (IsPressed == WasPressed)
		{
			EventType = InputKeyEventType.None;
		}
		else if (IsPressed && !WasPressed)
		{
			EventType = InputKeyEventType.Clicked;
		}
		else if (!IsPressed && WasPressed)
		{
			EventType = InputKeyEventType.Released;
		}

		return EventType != InputKeyEventType.None;
	}

	#endregion
}
