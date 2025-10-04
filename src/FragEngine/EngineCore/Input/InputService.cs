using FragEngine.EngineCore.Input.Axes;
using FragEngine.EngineCore.Input.Keys;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Extensions.Veldrid;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.EngineCore.Input;

/// <summary>
/// Engine service that handles input events and axes.
/// </summary>
public sealed class InputService
{
	#region Events

	/// <summary>
	/// Event that is triggered whenever a new input axis is created and registered.
	/// </summary>
	public event FuncInputAxisAdded? AxisAdded;

	/// <summary>
	/// Event that is triggered whenever an existing input axis is unregistered and removed.
	/// </summary>
	public event FuncInputAxisRemoved? AxisRemoved;

	#endregion
	#region Fields

	internal readonly ILogger logger;
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

	/// <summary>
	/// Gets the total number of input axes that are currently registered.
	/// </summary>
	public int AxisCount => axes.Count;
	/// <summary>
	/// Gets the total number of keyboard events that took place since last frame.
	/// </summary>
	public int KeyEventCount => keyEvents.Count;

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new input service instance.
	/// </summary>
	/// <param name="_logger">The logging service singleton.</param>
	/// <param name="_timeService">The time service singleton.</param>
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
				keyEvents.TryAdd(keyState.key, keyState.EventType);
			}
		}

		foreach (InputKeyState keyState in keyStates)
		{
			if (keyState.VersionIdx != versionIdx && keyState.UpdateState(false, versionIdx))
			{
				keyEvents.TryAdd(keyState.key, keyState.EventType);
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

	#endregion
	#region Methods Keys

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

	/// <summary>
	/// Gets a map of all keyboard events that took place since the last frame.
	/// </summary>
	/// <returns>A read-only map of keys and their events.</returns>
	public IReadOnlyDictionary<Key, InputKeyEventType> GetAllKeyEvent() => keyEvents;

	#endregion
	#region Methods Axes

	/// <summary>
	/// Registers a new input axis that maps to 2 keyboard keys.
	/// </summary>
	/// <param name="_name">The unique identifier name of the axis. May not be null or blank.</param>
	/// <param name="_negativeKey">The key that maps to axis value -1.</param>
	/// <param name="_positiveKey">The key that maps to axis value +1.</param>
	/// <param name="_outNewAxis">Outputs a new input axis. Returns an invalid axis on failure.</param>
	/// <param name="_useSnapshotEvents">Whether to use input snapshot events to update axis values.
	/// If false, final values from key states are used instead. Snapshot events may be slightly
	/// more granular and account for inter-frame presses and releases. False by default.</param>
	/// <returns>True if the new axis could be registered and is valid, false otherwise.</returns>
	public bool AddInputAxis(string _name, Key _negativeKey, Key _positiveKey, out InputAxis _outNewAxis, bool _useSnapshotEvents = false)
	{
		if (!CheckIfInputAxisCanBeAdded(_name))
		{
			_outNewAxis = InputAxis.Invalid;
			return false;
		}

		if (!_negativeKey.IsValid() || !_positiveKey.IsValid())
		{
			logger.LogError($"Cannot add input axis with invalid keyboard buttons! (Negative: '{_negativeKey}', Positive: '{_positiveKey}')");
			_outNewAxis = InputAxis.Invalid;
			return false;
		}

		_outNewAxis = new KeyboardAxis(_name, _negativeKey, _positiveKey, keyStates, _useSnapshotEvents);
		if (!_outNewAxis.IsValid)
		{
			logger.LogError($"Cannot add input axis; initial state is invalid! (Negative: '{_negativeKey}', Positive: '{_positiveKey}')");
			return false;
		}

		axes.Add(_name, _outNewAxis);
		AxisAdded?.Invoke(_outNewAxis);
		return true;
	}

	private bool CheckIfInputAxisCanBeAdded(string _name)
	{
		if (string.IsNullOrWhiteSpace(_name))
		{
			logger.LogError("Cannot add input axis with null or blank name!");
			return false;
		}

		if (axes.ContainsKey(_name))
		{
			logger.LogError($"An input axis with name '{_name}' already exists!");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Unregisters an existing input axis.
	/// </summary>
	/// <param name="_name">The unique identifier name of the axis.</param>
	/// <returns>True if an axis with that name existed and was unregistered, false otherwise.</returns>
	public bool RemoveInputAxis(string _name)
	{
		if (string.IsNullOrEmpty(_name))
		{
			return false;
		}

		bool removed = axes.Remove(_name, out InputAxis? removedAxis);
		if (removed)
		{
			AxisRemoved?.Invoke(removedAxis!);
		}
		else
		{
			logger.LogWarning($"No input axis with name '{_name}' exists, cannot remove it.", LogEntrySeverity.Trivial);
		}
		return removed;
	}

	/// <summary>
	/// Gets the state object of a specific input axis.
	/// </summary>
	/// <remarks>
	/// You may keep a reference to this object around, to query the same input axis on demand.
	/// </remarks>
	/// <param name="_name">The unique identifier name of the axis. May not be null or blank.</param>
	/// <returns>An input axis, or <see cref="InputAxis.Invalid"/>, if no valid axis with this name was found.</returns>
	public InputAxis GetInputAxis(string _name)
	{
		return !string.IsNullOrEmpty(_name) && axes.TryGetValue(_name, out InputAxis? axis)
			? axis
			: InputAxis.Invalid;
	}

	/// <summary>
	/// Gets the current value of an input axis.
	/// </summary>
	/// <param name="_name">The unique identifier name of the axis.</param>
	/// <returns>The current value of the axis, in the range from [-1;1]. Returns 0 if no axis with that name was found.</returns>
	public float GetAxisValue(string _name) => GetInputAxis(_name).CurrentValue;

	/// <summary>
	/// Gets the names of all input axes that are currently registered.
	/// </summary>
	/// <returns>A read-only collection of axis names.</returns>
	public IReadOnlyCollection<string> GetAllAxisNames() => axes.Keys;

	#endregion
}
