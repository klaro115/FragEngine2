namespace FragEngine.EngineCore.StateMachine;

internal sealed class RunningState(Engine _engine) : MainLoopEngineState(_engine)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Running;

	#endregion
	#region Methods

	//TODO

	#endregion
}
