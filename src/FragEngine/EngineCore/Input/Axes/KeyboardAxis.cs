using Veldrid;

namespace FragEngine.EngineCore.Input.Axes;

/// <summary>
/// Input axis type that uses keyboard keys as input signals. A pair of one negative and one
/// positive button make up the axis. Values are discrete by default, but linear interpolation
/// is supported as a form of temporal smoothing.
/// </summary>
/// <param name="_negativeKey">The key that maps to axis value -1.</param>
/// <param name="_positveKey">The key that maps to axis value +1.</param>
internal sealed class KeyboardAxis(string _name, Key _negativeKey, Key _positveKey) : InputAxis(_name)
{
	#region Fields

	public readonly Key negativeKey = _negativeKey;
	public readonly Key positveKey = _positveKey;

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

		if (_snapshot is not null)
		{
			int eventCount = 0;

			for (int i = _snapshot.KeyEvents.Count; i >= 0; i--)
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

		// In discrete mode, snap directly to target value:
		if (ValuesAreDiscrete)
		{
			rawCurrentValue = targetValue;
			CurrentValue = targetValue;
		}
		// In interpolated mode, smmothly transition to target value:
		else
		{
			float k = interpolationRate * _deltatime;
			rawCurrentValue = (1.0f - k) * rawCurrentValue + k * targetValue;

			// Snap to maximum value when within 0.05% of it:
			if (rawCurrentValue >= 0.995f)
			{
				CurrentValue = 1;
			}
			else if (CurrentValue <= -0.995f)
			{
				CurrentValue = -1;
			}
			else if (Math.Abs(CurrentValue) < DeadZone)
			{
				CurrentValue = 0;
			}
			else
			{
				CurrentValue = rawCurrentValue;
			}
		}
	}

	#endregion
}
