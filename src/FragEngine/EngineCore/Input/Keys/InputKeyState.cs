using Veldrid;

namespace FragEngine.EngineCore.Input.Keys;

/// <summary>
/// Object tracking the state of a single keyboard button.
/// </summary>
public sealed class InputKeyState
{
	#region Fields

	/// <summary>
	/// The keyboard button whose state is represented by this instance.
	/// </summary>
	public readonly Key key;

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
	public InputKeyEventType EventType { get; private set; } = InputKeyEventType.Unchanged;

	internal uint VersionIdx { get; private set; } = 0u;

	/// <summary>
	/// Gets an invalid keyboard key state. Use this as a placeholder if you don't want to bother with nullables in your input logic.
	/// </summary>
	public static InputKeyState Invalid => new(Key.Unknown)
	{
		IsPressed = false,
		WasPressed = false,
		EventType = InputKeyEventType.Unchanged,
		VersionIdx = uint.MaxValue,
	};

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new state object for a specfic key.
	/// </summary>
	/// <param name="_key">A keyboard key.</param>
	internal InputKeyState(Key _key)
	{
		key = _key;
	}

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
			EventType = InputKeyEventType.Unchanged;
		}
		else if (IsPressed && !WasPressed)
		{
			EventType = InputKeyEventType.Clicked;
		}
		else if (!IsPressed && WasPressed)
		{
			EventType = InputKeyEventType.Released;
		}

		return EventType != InputKeyEventType.Unchanged;
	}

	#endregion
}
