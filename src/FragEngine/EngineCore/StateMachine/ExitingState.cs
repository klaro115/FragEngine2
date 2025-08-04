using FragEngine.Application;
using FragEngine.Logging;

namespace FragEngine.EngineCore.StateMachine;

internal sealed class ExitingState(Engine _engine, IAppLogic _appLogic) : EngineState(_engine, _appLogic)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Exiting;

	#endregion
	#region Methods

	public override bool Initialize()
	{
		if (IsDisposed)
		{
			engine.Logger.LogError("Cannot initialize engine state that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}
		if (engine.IsDisposed)
		{
			engine.Logger.LogError("Cannot initialize engine state of engine instance that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}

		//...

		return true;
	}

	public override void Shutdown()
	{
		if (!engine.WindowService.IsDisposed)
		{
			engine.WindowService.CloseAllWindows();
		}

		if (!engine.Graphics.IsDisposed)
		{
			engine.Graphics.Shutdown();
		}

		appLogic.Shutdown();
	}

	public override bool Run(CancellationToken token)
	{
		if (IsDisposed)
		{
			engine.Logger.LogError("Cannot run engine state that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}
		if (engine.IsDisposed)
		{
			engine.Logger.LogError("Cannot run engine state of engine instance that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}

		//...

		return true;
	}

	#endregion
}
