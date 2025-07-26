namespace FragEngine.Engine;

public delegate void FuncEngineStateChanging(EngineState _currentState, EngineState _targetState);
public delegate void FuncEngineStateChanged(EngineState _previousState, EngineState _newState);
