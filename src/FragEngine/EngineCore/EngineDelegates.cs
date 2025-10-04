namespace FragEngine.EngineCore;

/// <summary>
/// Delegate for additional initialization logic that should run immediately after the engine's constructor has finished.
/// </summary>
/// <remarks>
/// NOTE: This method will be called at most once, and allows you to perform additional initialization on the engine's
/// services before the state machine starts. In most cases, it would be better and safer to initialize additional logic
/// from within the engine's startup state.
/// </remarks>
/// <param name="_engine">The fully initialized engine instance.</param>
/// <returns>True if external initialization was successful, false if a breaking issue arose.<para/>
/// WARNING: If this returns false, the engine's constructor will throw an exception and try to dispose itself safely.</returns>
public delegate bool FuncEnginePostInitialization(Engine _engine);

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
