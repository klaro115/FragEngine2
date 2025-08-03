using Veldrid;

namespace FragEngine.EngineCore.Input.Axes;

internal sealed class KeyboardAxis : InputAxis
{
	#region Fields

	private InputAxisType type = InputAxisType.Discrete;

	#endregion
	#region Properties

	public override InputAxisType Type => type;

	public override bool ValuesAreDiscrete
	{
		get => type == InputAxisType.Discrete;
		set => SetInputType(value ? InputAxisType.Discrete : InputAxisType.Interpolated);
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
		throw new NotImplementedException();
	}

	#endregion
}
