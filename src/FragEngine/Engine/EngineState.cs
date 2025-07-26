namespace FragEngine.Engine;

/// <summary>
/// Enum with the different main states of the engine's central state machine.
/// </summary>
public enum EngineState
{
	None,

	Starting,
	Loading,
	Running,
	Unloading,
	Exiting,
}

/// <summary>
/// Extension methods for the <see cref="EngineState"/> enum.
/// </summary>
public static class EngineStateExt
{
	#region Methods

	/// <summary>
	/// Gets whether the engine's main loop is running during this state.
	/// </summary>
	/// <param name="_state">This state.</param>
	/// <returns>True if the main loop is running, false otherwise.</returns>
	public static bool IsRunningMainLoop(this EngineState _state)
	{
		bool isMainLoop = _state == EngineState.Loading || _state == EngineState.Running || _state == EngineState.Unloading;
		return isMainLoop;
	}

	/// <summary>
	/// Checks whether this state can transition directly to a specific target state.
	/// </summary>
	/// <param name="_currentState">The current state.</param>
	/// <param name="_targetState">The target state that we wish to transition to.</param>
	/// <returns>True if a direct transition is possible, false otherwise.</returns>
	public static bool CanTransitionToState(this EngineState _currentState, EngineState _targetState)
	{
		if (_currentState == _targetState)
		{
			return true;
		}

		var canTransition = _currentState switch
		{
			EngineState.None => _targetState == EngineState.Starting,
			EngineState.Starting => _targetState == EngineState.Loading,
			EngineState.Loading => _targetState == EngineState.Running,
			EngineState.Running => _targetState == EngineState.Unloading,
			EngineState.Unloading => _targetState == EngineState.Exiting,
			EngineState.Exiting => _targetState == EngineState.None,
			_ => false,
		};
		return canTransition;
	}

	#endregion
}
