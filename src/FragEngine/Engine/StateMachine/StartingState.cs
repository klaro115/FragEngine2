using FragEngine.Logging;

namespace FragEngine.Engine.StateMachine;

internal sealed class StartingState(Engine _engine) : IEngineState
{
	#region Fields

	private readonly Engine engine = _engine;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	public EngineState State => EngineState.Starting;

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}
	private void Dispose(bool _)
	{
		IsDisposed = true;
	}

	public bool Initialize()
	{
		if (IsDisposed)
		{
			engine.Logger.LogError("Cannot initialize engine state that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}

		//...

		return true;
	}

	public void Shutdown()
	{
		//...
	}

	public bool Run(CancellationToken token)
	{
		if (IsDisposed)
		{
			engine.Logger.LogError("Cannot run engine state that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}

		//...

		return true;
	}

	#endregion
}
