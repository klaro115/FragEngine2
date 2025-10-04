namespace FragEngine.EngineCore;

/// <summary>
/// Enum with the different main states of the engine's central state machine.
/// </summary>
public enum EngineStateType
{
	/// <summary>
	/// Neutral/Off state.
	/// </summary>
	None,

	/// <summary>
	/// Engine is starting up, configurations are loaded, core services initialized.
	/// </summary>
	Starting,
	/// <summary>
	/// Resources are gathered and core resources loaded. In this state, a loading screen should be shown.
	/// </summary>
	Loading,
	/// <summary>
	/// Fully operational running state. In this state, the main application logic will happen.
	/// </summary>
	Running,
	/// <summary>
	/// Resources are being unloaded, and data persisted. In this state, an unloading/goodbye screen may be shown.
	/// </summary>
	Unloading,
	/// <summary>
	/// Engine is shutting down, core systems are terminated, final logs are written.
	/// </summary>
	Exiting,
}

/// <summary>
/// Extension methods for the <see cref="EngineStateType"/> enum.
/// </summary>
public static class EngineStateTypeExt
{
	#region Methods

	/// <summary>
	/// Gets whether the engine's main loop is running during this state.
	/// </summary>
	/// <param name="_state">This state.</param>
	/// <returns>True if the main loop is running, false otherwise.</returns>
	public static bool IsRunningMainLoop(this EngineStateType _state)
	{
		bool isMainLoop = _state == EngineStateType.Loading || _state == EngineStateType.Running || _state == EngineStateType.Unloading;
		return isMainLoop;
	}

	/// <summary>
	/// Checks whether this state can transition directly to a specific target state.
	/// </summary>
	/// <param name="_currentState">The current state.</param>
	/// <param name="_targetState">The target state that we wish to transition to.</param>
	/// <returns>True if a direct transition is possible, false otherwise.</returns>
	public static bool CanTransitionToState(this EngineStateType _currentState, EngineStateType _targetState)
	{
		if (_currentState == _targetState)
		{
			return true;
		}

		var canTransition = _currentState switch
		{
			EngineStateType.None => _targetState == EngineStateType.Starting,
			EngineStateType.Starting => _targetState == EngineStateType.Loading,
			EngineStateType.Loading => _targetState == EngineStateType.Running,
			EngineStateType.Running => _targetState == EngineStateType.Unloading,
			EngineStateType.Unloading => _targetState == EngineStateType.Exiting,
			EngineStateType.Exiting => _targetState == EngineStateType.None,
			_ => false,
		};
		return canTransition;
	}

	#endregion
}
