using FragEngine.EngineCore.Input.Keys;
using Veldrid;

namespace FragEngine.EngineCore.Input.Axes;

/// <summary>
/// Input axis type that uses keyboard keys as input signals. A pair of one negative and one
/// positive button make up the axis. Values are discrete by default, but linear interpolation
/// is supported as a form of temporal smoothing.
/// </summary>
public sealed class KeyboardAxis : InputAxis
{
	#region Fields

	public readonly Key negativeKey;
	public readonly Key positveKey;

	private readonly InputKeyState negativeState;
	private readonly InputKeyState positiveState;

	private readonly bool useSnapshot;
	private InputAxisType type = InputAxisType.Discrete;

	private float interpolationRate = 3.0f;
	private float rawCurrentValue = 0.0f;

	#endregion
	#region Properties

	public override InputAxisType Type => type;

	public override bool ValuesAreDiscrete
	{
		get => type == InputAxisType.Discrete;
		set => SetInputType(value ? InputAxisType.Discrete : InputAxisType.Interpolated);
	}

	/// <summary>
	/// Gets or sets the rate at which the current axis value transitions to the target value.
	/// This only applies if <see cref="Type"/> is set to <see cref="InputAxisType.Interpolated"/>.
	/// </summary>
	public float InterpolationRate
	{
		get => interpolationRate;
		set => interpolationRate = Math.Clamp(value, 0.01f, 1000.0f);
	}

	public override bool IsValid => negativeState.IsValid && positiveState.IsValid && Type != InputAxisType.Analog;

	#endregion
	#region Constructors

	/// <summary>
	/// Create a new keyboard axis instance.
	/// </summary>
	/// <param name="_name">The name of this input axis. This serves as a unique identifier.</param>
	/// <param name="_negativeKey">The key that maps to axis value -1.</param>
	/// <param name="_positveKey">The key that maps to axis value +1.</param>
	/// <param name="_keyStates">The input service's array of keyboard key states.</param>
	/// <param name="_useSnapshot">Whether to use input snapshot events to update axis values.
	/// If false, final values from key states are used instead. Snapshot events may be slightly
	/// more granular and account for inter-frame presses and releases. False by default.</param>
	/// <exception cref="ArgumentNullException">Input key states may not be null.</exception>
	public KeyboardAxis(string _name, Key _negativeKey, Key _positveKey, InputKeyState[] _keyStates, bool _useSnapshot = false) : base(_name)
	{
		ArgumentNullException.ThrowIfNull(_keyStates);

		negativeKey = _negativeKey;
		positveKey = _positveKey;

		negativeState = _keyStates[(int)_negativeKey];
		positiveState = _keyStates[(int)_positveKey];

		useSnapshot = _useSnapshot;
	}

	#endregion
	#region Methods

	public override bool SetInputType(InputAxisType _newType)
	{
		if (_newType == InputAxisType.Analog)
		{
			return false;
		}

		type = _newType;
		return true;
	}

	internal override void Update(InputSnapshot? _snapshot, float _deltatime)
	{
		PreviousValue = CurrentValue;

		// Determine target axis value based on key press/release events over the last frame:
		float targetValue = 0.0f;

		if (useSnapshot && _snapshot is not null)
		{
			int eventCount = 0;

			for (int i = _snapshot.KeyEvents.Count - 1; i >= 0; i--)
			{
				Key key = _snapshot.KeyEvents[i].Key;
				if (key == negativeKey)
				{
					targetValue += _snapshot.KeyEvents[i].Down ? -1 : 0;
					eventCount++;
				}
				else if (key == positveKey)
				{
					targetValue += _snapshot.KeyEvents[i].Down ? 1 : 0;
					eventCount++;
				}
			}

			targetValue /= Math.Max(eventCount, 1);
		}
		else
		{
			targetValue += negativeState.IsPressed ? -1 : 0;
			targetValue += positiveState.IsPressed ? 1 : 0;
		}

		// In discrete mode, snap directly to target value:
		if (ValuesAreDiscrete)
		{
			rawCurrentValue = targetValue;
			CurrentValue = targetValue;
		}
		// In interpolated mode, smoothly transition to target value:
		else
		{
			float maxChange = interpolationRate * _deltatime;
			float targetDiff = targetValue - rawCurrentValue;
			float change = Math.Clamp(targetDiff, -maxChange, maxChange);
			rawCurrentValue = Math.Clamp(rawCurrentValue + change, -1, 1);

			// Snap to maximum value when within 0.05% of it:
			if (rawCurrentValue >= 0.995f)
			{
				CurrentValue = 1;
			}
			else if (rawCurrentValue <= -0.995f)
			{
				CurrentValue = -1;
			}
			else if (Math.Abs(rawCurrentValue) < DeadZone)
			{
				CurrentValue = 0;
			}
			else
			{
				CurrentValue = rawCurrentValue;
			}
		}

		if (positveKey == Key.D)
		{
			Console.WriteLine($"Keyboard Axis ({negativeKey}/{positveKey}), Value={CurrentValue:0.000}, Raw={rawCurrentValue:0.000}");
		}
	}

	internal override void ResetState()
	{
		base.ResetState();

		rawCurrentValue = 0;
	}

	public override string ToString() => $"Keyboard Axis ({negativeKey}/{positveKey}), Value={CurrentValue:0.000}";

	#endregion
}
