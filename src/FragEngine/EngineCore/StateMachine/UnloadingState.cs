namespace FragEngine.EngineCore.StateMachine;

internal sealed class UnloadingState(Engine _engine) : MainLoopEngineState(_engine)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Unloading;

	#endregion
	#region Methods

	//TODO

	#endregion
}
