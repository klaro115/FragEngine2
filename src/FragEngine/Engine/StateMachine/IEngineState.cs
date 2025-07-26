namespace FragEngine.Engine.StateMachine;

internal interface IEngineState : IDisposable
{
	#region Properties

	bool IsDisposed { get; }

	EngineState State { get; }

	#endregion
	#region Methods

	bool Initialize();
	void Shutdown();

	bool Run(CancellationToken token);

	#endregion
}
