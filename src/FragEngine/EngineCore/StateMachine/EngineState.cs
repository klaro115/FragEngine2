using FragEngine.Application;
using FragEngine.Interfaces;

namespace FragEngine.EngineCore.StateMachine;

/// <summary>
/// Base class for the states of the engine's main statemachine.
/// </summary>
/// <param name="_engine">The engine instance.</param>
internal abstract class EngineState(Engine _engine, IAppLogic _appLogic) : IExtendedDisposable
{
	#region Fields

	protected readonly Engine engine = _engine;
	protected readonly IAppLogic appLogic = _appLogic;

	#endregion
	#region Properties

	public bool IsDisposed { get; protected set; } = false;

	/// <summary>
	/// Gets the state type that this instance represents.
	/// </summary>
	public abstract EngineStateType State { get; }

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	protected virtual void Dispose(bool _disposing)
	{
		IsDisposed = true;
	}

	/// <summary>
	/// Initializes the state.
	/// </summary>
	/// <returns>True if the state could be initialized successfully, false on failure.</returns>
	public abstract bool Initialize();

	/// <summary>
	/// Shuts down the state and releases transient resources.
	/// </summary>
	public abstract void Shutdown();

	/// <summary>
	/// Runs the state's main logic.
	/// </summary>
	/// <param name="_token">A cancellation token that either interrupts and terminates the state prematurely, or ends a running main loop.</param>
	/// <returns>True if the state's logic ran through, false if breaking issues occurred.</returns>
	public abstract bool Run(CancellationToken _token);

	#endregion
}
