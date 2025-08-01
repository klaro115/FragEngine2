using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.EngineCore.StateMachine;

internal sealed class StartingState(Engine _engine) : EngineState(_engine)
{
	#region Properties

	public override EngineStateType State => EngineStateType.Starting;

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
		//...
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

		EngineConfig config = engine.Provider.GetRequiredService<EngineConfig>();

		if (!engine.Graphics.Initialize(config.CreateMainWindowImmediately))
		{
			engine.Logger.LogError("Failed to initialize graphics system!");
			return false;
		}

		//...

		return true;
	}

	#endregion
}
