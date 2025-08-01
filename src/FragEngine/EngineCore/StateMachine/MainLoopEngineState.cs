using FragEngine.Logging;
using Veldrid;

namespace FragEngine.EngineCore.StateMachine;

/// <summary>
/// Abstract base class for engine states that operate a main loop.
/// </summary>
/// <param name="_engine">The engine instance.</param>
internal abstract class MainLoopEngineState(Engine _engine) : EngineState(_engine)
{
	#region Fields

	private bool mainLoopIsRunning = false;
	private CancellationTokenSource? internalCancellationSource = null;

	#endregion
	#region Methods

	protected override void Dispose(bool _disposing)
	{
		WaitForMainLoopToExit();

		base.Dispose(_disposing);
	}

	public override bool Initialize()
	{
		if (IsDisposed)
		{
			engine.Logger.LogError("Cannot initialize main loop state that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}
		if (engine.IsDisposed)
		{
			engine.Logger.LogError("Cannot initialize main loop state of engine instance that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}
		if (!engine.IsInMainLoop)
		{
			engine.Logger.LogError("Cannot initialize main loop; engine is not currently in a main loop state!", LogEntrySeverity.Critical);
			return false;
		}
		if (mainLoopIsRunning)
		{
			engine.Logger.LogError("Cannot initialize main loop state that is already running!", LogEntrySeverity.High);
			return false;
		}

		internalCancellationSource = new();

		return true;
	}

	public override void Shutdown()
	{
		WaitForMainLoopToExit();
	}

	private void WaitForMainLoopToExit()
	{
		// Signal (external) termination of the main loop state:
		internalCancellationSource?.Cancel();

		// Wait around until the main loop has exited:
		int i = 0;
		while (i < 50 && mainLoopIsRunning)
		{
			Thread.Sleep(10);
		}

		// Cleanup:
		internalCancellationSource?.Dispose();
	}

	public override bool Run(CancellationToken _token)
	{
		if (IsDisposed)
		{
			engine.Logger.LogError("Cannot run main loop state that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}
		if (engine.IsDisposed)
		{
			engine.Logger.LogError("Cannot run main loop state of engine instance that has already been disposed!", LogEntrySeverity.Critical);
			return false;
		}

		bool success = RunMainLoop(_token, internalCancellationSource!.Token);
		return success;
	}

	/// <summary>
	/// Starts up the main loop. This will block the calling thread until the engine exits.
	/// </summary>
	/// <param name="_exitToken">A cancellation token, telling the loop to exit. This is issued by the engine itself.</param>
	/// <param name="_abortToken">A cancellation token, telling the loop to exit. This is issued by internal lifecycle mechanisms.</param>
	/// <returns>True if the main loop was started and ran without issues, false otherwise.</returns>
	protected bool RunMainLoop(CancellationToken _exitToken, CancellationToken _abortToken)
	{
		bool success = true;

		mainLoopIsRunning = true;

		try
		{
			while (!IsDisposed && engine.IsRunning && !_exitToken.IsCancellationRequested && !_abortToken.IsCancellationRequested)
			{
				success &= ExecuteUpdateCycle(_exitToken);
			}
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Main loop encountered an unhandled exception! Exiting...", ex, LogEntrySeverity.Critical);

			internalCancellationSource?.Cancel();
			success = false;
		}

		mainLoopIsRunning = false;

		return success;
	}

	/// <summary>
	/// Performs a single input-update-draw cycle. This is executed for each iteration of the main loop.
	/// </summary>
	/// <param name="_token">A cancellation token, telling complex processes to exit.</param>
	/// <returns>True if the update cycle ran without issues, false otherwise.</returns>
	protected virtual bool ExecuteUpdateCycle(CancellationToken _token)
	{
		// Update window logic, capture input events:
		if (!engine.WindowService.Update(out InputSnapshot? inputSnapshot))
		{
			return false;
		}

		//TODO 1: Implement and update input service.
		//TODO 2: Do application logic.
		//  TODO 2.1: Input
		//  TODO 2.2: Update
		//  TODO 2.3: Draw

		return true;
	}

	#endregion
}
