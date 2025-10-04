using FragEngine.Application;

namespace FragEngine.EngineCore.StateMachine;

internal sealed class RunningState(Engine _engine, IAppLogic _appLogic) : MainLoopEngineState(_engine, _appLogic)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Running;

	#endregion
	#region Methods

	protected override bool ExecuteUpdateCycle(CancellationToken _token)
	{
		// Process input:
		if (!appLogic.UpdateRunningState_Input())
		{
			return false;
		}

		// Process logic:
		if (!appLogic.UpdateRunningState_Update())
		{
			return false;
		}

		// Process rendering:
		if (!appLogic.UpdateRunningState_Draw())
		{
			return false;
		}

		return true;
	}

	#endregion
}
