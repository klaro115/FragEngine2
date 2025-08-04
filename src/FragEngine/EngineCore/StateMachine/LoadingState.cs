using FragEngine.Application;

namespace FragEngine.EngineCore.StateMachine;

internal sealed class LoadingState(Engine _engine, IAppLogic _appLogic) : MainLoopEngineState(_engine, _appLogic)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Loading;

	#endregion
	#region Methods

	protected override bool ExecuteUpdateCycle(CancellationToken _token)
	{
		bool success = appLogic.UpdateLoadingState(out bool _outLoadingIsDone);

		if (_outLoadingIsDone)
		{
			internalCancellationSource?.Cancel();
		}
		return success;
	}

	#endregion
}
