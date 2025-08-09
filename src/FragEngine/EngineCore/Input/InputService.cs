using FragEngine.EngineCore.Input.Axes;
using FragEngine.EngineCore.Input.Keys;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.EngineCore.Input;

/// <summary>
/// Engine service that handles input events and axes.
/// </summary>
/// <param name="_logger">The logging service singleton.</param>
/// <param name="_timeService">The time service singleton.</param>
public sealed class InputService
{
	#region Fields

	private readonly ILogger logger;
	private readonly TimeService timeService;

	private readonly InputKeyState[] keyStates;
	private readonly Dictionary<Key, InputKeyEventType> keyEvents = [];

	private readonly Dictionary<string, InputAxis> axes = [];

	private uint versionIdx = 0u;

	#endregion
	#region Constants

	private const int maximumKeyCount = (int)Key.LastKey + 1;

	#endregion
	#region Properties

	public int AxisCount => axes.Count;

	#endregion
	#region Constructors

	public InputService(ILogger _logger, TimeService _timeService)
	{
		logger = _logger;
		timeService = _timeService;

		logger.LogStatus("# Initializing input service.");

		keyStates = new InputKeyState[maximumKeyCount];
		for (int i = 0; i < maximumKeyCount; i++)
		{
			InputKeyState keyState = new((Key)i);
			keyStates[i] = keyState;
			keyState.UpdateState(false, versionIdx);
		}

		logger.LogMessage("- Input service initialized.");
	}

	#endregion
	#region Methods

	/// <summary>
	/// Resets and initializes all input signals for an upcoming frame.
	/// </summary>
	/// <remarks>
	/// This method should only be called by the <see cref="WindowService"/>.
	/// The snapshot will be null unless one of the application windows is focused.
	/// </remarks>
	/// <param name="snapshot">A snapshot of the input events since last frame.</param>
	/// <returns>True if inputs were updated, false on failure.</returns>
	internal bool UpdateInputSnapshot(InputSnapshot? snapshot)
	{
		versionIdx++;
		
		float deltaTime = timeService.AppDeltaTimeSeconds;

		if (!UpdateKeyStates(snapshot))
		{
			return false;
		}

		if (!UpdateAxes(snapshot, deltaTime))
		{
			logger.LogError("Failed to update input axes!");
			return false;
		}

		//TODO 1 [later]: Add support for typed string events.
		//TODO 2 [later]: Add support for controllers & gamepads.

		return true;
	}

	private bool UpdateKeyStates(InputSnapshot? snapshot)
	{
		keyEvents.Clear();

		if (snapshot is null || snapshot.KeyEvents.Count == 0)
		{
			foreach (InputKeyState key in keyStates)
			{
				key.UpdateState(false, versionIdx);
			}
			return true;
		}

		foreach (KeyEvent keyEvent in snapshot.KeyEvents)
		{
			int keyIdx = (int)keyEvent.Key;
			InputKeyState keyState = keyStates[keyIdx];

			if (keyState.UpdateState(keyEvent.Down, versionIdx))
			{
				keyEvents.Add(keyState.key, keyState.EventType);
			}
		}

		foreach (InputKeyState keyState in keyStates)
		{
			if (keyState.VersionIdx != versionIdx && keyState.UpdateState(false, versionIdx))
			{
				keyEvents.Add(keyState.key, keyState.EventType);
			}
		}

		return true;
	}

	private bool UpdateAxes(InputSnapshot? snapshot, float deltaTime)
	{
		foreach (var axis in axes)
		{
			axis.Value.Update(snapshot, deltaTime);
		}

		return true;
	}

	public bool AddAxis(string _name, Key _negativeKey, Key _positiveKey)
	{
		//TODO
		return false;	//TEMP
	}

	#endregion
	#region Methods Getters

	/// <summary>
	/// Gets the state object of a specific keyboard button.
	/// </summary>
	/// <remarks>
	/// You may keep a reference to this object around, to query the same key's state on demand.
	/// </remarks>
	/// <param name="_key">A keyboard key.</param>
	/// <returns>A state object for a specfic key, or <see cref="InputKeyState.Invalid"/>, if the given key value was invalid.</returns>
	public InputKeyState GetKeyState(Key _key)
	{
		int keyIndex = (int)_key;
		InputKeyState? keyState = keyIndex >= 0 && keyIndex < maximumKeyCount
			? keyStates[keyIndex]
			: InputKeyState.Invalid;
		return keyState;
	}

	/// <summary>
	/// Gets whether a keyboard key is currently pressed.
	/// </summary>
	/// <param name="_key">A keyboard key.</param>
	/// <returns>True if pressed down, false if up.</returns>
	public bool GetKey(Key _key) => GetKeyState(_key).IsPressed;

	/// <summary>
	/// Gets whether a keyboard key was just pressed down.
	/// </summary>
	/// <param name="_key">A keyboard key.</param>
	/// <returns>True if the key was just pressed down, false otherwise.</returns>
	public bool GetKeyDown(Key _key) => GetKeyState(_key).EventType == InputKeyEventType.Clicked;

	/// <summary>
	/// Gets whether a keyboard key was just released.
	/// </summary>
	/// <param name="_key">A keyboard key.</param>
	/// <returns>True if the key was just released, false otherwise.</returns>
	public bool GetKeyUp(Key _key) => GetKeyState(_key).EventType == InputKeyEventType.Released;

	#endregion
}
