using FragEngine.Engine.StateMachine;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.Engine;

public sealed class Engine : IDisposable
{
	#region Events

	public event FuncEngineStateChanging? StateChanging;
	public event FuncEngineStateChanged? StateChanged;

	#endregion
	#region Fields

	// SYNCHRONIZATION:

	private CancellationTokenSource exitCancellationSource = new();
	private SemaphoreSlim engineStateSemaphore = new(1, 1);

	// STATE MACHINE:

	private IEngineState? CurrentState = null;
	private StartingState StartingState;
	//...

	#endregion
	#region Properties

	// ENGINE STATE:

	public bool IsDisposed { get; private set; } = false;
	public bool IsRunning { get; private set; } = false;
	public bool IsExiting => exitCancellationSource.IsCancellationRequested;
	public bool IsInMainLoop => State.IsRunningMainLoop();

	/// <summary>
	/// Gets the current state of the engine's main statemachine.
	/// </summary>
	public EngineState State { get; private set; } = EngineState.None;

	// CORE SERVICES:

	public IServiceProvider Provider { get; }
	public ILogger Logger { get; }
	//...

	#endregion
	#region Constructors

	public Engine(IServiceProvider? _serviceProvider = null)
	{
		// Initialize DI:
		if (_serviceProvider is null)
		{
			ServiceCollection services = new();
			services.AddSingleton<ILogger, ConsoleLogger>();
			//...

			_serviceProvider = services.BuildServiceProvider();;
		}

		Provider = _serviceProvider;

		// Retrieve core services for easier access:
		Logger = Provider.GetService<ILogger>() ?? new ConsoleLogger();
		//...

		// Create state machine states:
		StartingState = new(this);
		//...
		CurrentState = StartingState;
	}

	~Engine()
	{
		Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}
	private void Dispose(bool _)
	{
		if (IsRunning)
		{
			RequestExit();
		}

		IsRunning = false;
		IsDisposed = true;

		Logger.WriteToFile();

		if (Provider is IDisposable providerDisposable)
		{
			providerDisposable.Dispose();
		}

		exitCancellationSource?.Dispose();
	}

	public bool Run()
	{
		if (IsDisposed)
		{
			Logger.LogError("Cannot run engine that has already been disposed!");
			return false;
		}
		if (IsRunning)
		{
			Logger.LogError("Cannot run engine that has is already running!");
			return false;
		}
		if (IsExiting)
		{
			Logger.LogError("Cannot re-run engine that has is still exiting!");
			return false;
		}

		if (exitCancellationSource is not null)
		{
			exitCancellationSource?.Cancel();
			exitCancellationSource?.Dispose();
		}

		exitCancellationSource = new();

		bool success = true;
		bool isLoaded = false;

		// Run startup logic:
		success &= SetState(EngineState.Starting);
		if (success)
		{
			success &= CurrentState!.Run(exitCancellationSource.Token);
			IsRunning = success;
		}

		// Load core content:
		if (success)
		{
			success = SetState(EngineState.Loading);
			if (success)
			{
				isLoaded = CurrentState!.Run(exitCancellationSource.Token);
				success &= isLoaded;
			}
		}

		// Run main application logic:
		if (isLoaded)
		{
			success &= SetState(EngineState.Running);
			if (success)
			{
				success &= CurrentState!.Run(exitCancellationSource.Token);
			}
		}

		// Unload all content:
		if (isLoaded)
		{
			success &= SetState(EngineState.Unloading);
			if (success)
			{
				success &= CurrentState!.Run(exitCancellationSource.Token);
			}
		}

		// Run shutdown logic:
		IsRunning = false;
		success &= SetState(EngineState.Exiting);
		if (success)
		{
			success &= CurrentState!.Run(CancellationToken.None);
		}

		return success;
	}

	/// <summary>
	/// Request the engine to end the main thread and quit.
	/// </summary>
	public void RequestExit()
	{
		if (IsDisposed || !IsRunning || IsExiting) return;

		engineStateSemaphore.Wait();
		exitCancellationSource.Cancel();
		engineStateSemaphore.Release();

		Logger.LogMessage("Exit has been requested.");
	}

	private bool SetState(EngineState _newState)
	{
		if (IsDisposed)
		{
			Logger.LogError("Cannot change state of engine that has already been disposed!");
			return false;
		}

		// Inject unloading/exit state instead, if exit signal has been sent:
		if (IsExiting)
		{
			Logger.LogWarning($"Exit has been requested, aborting state change to '{_newState}'.");
			if (State == EngineState.Loading || State == EngineState.Running)
			{
				_newState = EngineState.Unloading;
			}
			else if (State == EngineState.Starting)
			{
				_newState = EngineState.Exiting;
			}
		}

		// Verify state transition:
		if (_newState == State)
		{
			Logger.LogWarning($"Engine is already in target state. (Current: '{State}')");
			return true;
		}
		if (!State.CanTransitionToState(_newState))
		{
			Logger.LogError($"Cannot transition to target state! (Current: '{State}', Target: '{_newState}')");
			return false;
		}

		// Notify listeners that the state is about to change:
		StateChanging?.Invoke(State, _newState);

		if (!EndCurrentState(_newState))
		{
			Logger.LogError($"Failed to shutdown current engine state! (Current: '{State}', Target: '{_newState}')", LogEntrySeverity.Critical);
			return false;
		}

		// Change engine state:
		engineStateSemaphore.Wait();
		EngineState prevState = State;
		State = _newState;
		engineStateSemaphore.Release();

		// Initialize new state:
		if (!StartNewState())
		{
			Logger.LogError($"Failed to initialize new engine state! (Current: '{State}')", LogEntrySeverity.Critical);
			return false;
		}

		// Notify listeners that the state has changed:
		StateChanged?.Invoke(prevState, State);
		return true;
	}

	private bool EndCurrentState(EngineState _newState)
	{
		if (!IsExiting && _newState == EngineState.Exiting)
		{
			engineStateSemaphore.Wait();
			exitCancellationSource.Cancel();
			engineStateSemaphore.Release();
		}

		if (CurrentState is not null && !CurrentState.IsDisposed)
		{
			CurrentState.Shutdown();
		}

		return true;
	}

	private bool StartNewState()
	{
		if (State == EngineState.Starting)
		{
			engineStateSemaphore.Wait();
			exitCancellationSource = new();
			engineStateSemaphore.Release();
		}

		switch (State)
		{
			case EngineState.Starting:
				CurrentState = StartingState;
				break;
			case EngineState.Loading:
				//TODO
				break;
			case EngineState.Running:
				//TODO
				break;
			case EngineState.Unloading:
				//TODO
				break;
			case EngineState.Exiting:
				//TODO
				break;
			case EngineState.None:
			default:
				CurrentState = null;
				break;
		}

		if (CurrentState is not null && !CurrentState.Initialize())
		{
			return false;
		}
		return true;
	}

	#endregion
}
