namespace FragEngine.EngineCore;

/// <summary>
/// Delegate for listener methods that respond when the engine's statemachine is about to transition to a different state.
/// </summary>
/// <param name="_currentState">The current engine state.</param>
/// <param name="_targetState">The state the engine is about to transition to.</param>
public delegate void FuncEngineStateChanging(EngineStateType _currentState, EngineStateType _targetState);

/// <summary>
/// Delegate for listener methods that respond when the engine's statemachine has transitioned to a different state.
/// </summary>
/// <param name="_previousState">The engine state we transitioned away from.</param>
/// <param name="_newState">The new state the engine is in now.</param>
public delegate void FuncEngineStateChanged(EngineStateType _previousState, EngineStateType _newState);
