using FragEngine.Application;

namespace FragEngine.EngineCore.StateMachine;

internal sealed class UnloadingState(Engine _engine, IAppLogic _appLogic) : MainLoopEngineState(_engine, _appLogic)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Unloading;

	#endregion
	#region Methods

	protected override bool ExecuteUpdateCycle(CancellationToken _token)
	{
		bool success = appLogic.UpdateUnloadingState(out bool _outUnloadingIsDone);

		if (_outUnloadingIsDone)
		{
			internalCancellationSource?.Cancel();
		}
		return success;
	}

	#endregion
}
