using FragEngine.EngineCore.StateMachine;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics;
using FragEngine.Helpers;
using FragEngine.Interfaces;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.EngineCore;

/// <summary>
/// The main FragmentEngine engine class. This type owns and manages all services and resources of the application.<para/>
/// The engine operates a state machine that boots up, loads resources, executes application code in the main loop,
/// then unloads resources, and exits. Call <see cref="Run"/> to start up the app.<para/>
/// The engine uses a dependency injection system to create and manage services and subsystems. Use the <see cref="Provider"/>
/// to retrieve or instantiate a service at run-time. Additional custom services may be registered through a service collection
/// that is passed to the engine's constructor.
/// </summary>
public sealed class Engine : IExtendedDisposable
{
	#region Events

	/// <summary>
	/// Event that is fired when the engine's state is about to change.
	/// </summary>
	public event FuncEngineStateChanging? StateChanging;
	/// <summary>
	/// Event that is fired when the engine's state has just changed.
	/// </summary>
	public event FuncEngineStateChanged? StateChanged;
	/// <summary>
	/// Event that is fired when shutdown has been requested. This event is not invoked on unexpected or crash-related exits.
	/// </summary>
	public event Action? ExitRequested;

	#endregion
	#region Fields

	// SYNCHRONIZATION:

	private CancellationTokenSource exitCancellationSource = new();
	private readonly SemaphoreSlim engineStateSemaphore = new(1, 1);

	// STATE MACHINE:

	private bool isRunning = false;
	private EngineState? currentState = null;

	private readonly StartingState startingState;
	private readonly LoadingState loadingState;
	private readonly RunningState runningState;
	private readonly UnloadingState unloadingState;
	private readonly ExitingState exitingState;

	#endregion
	#region Properties

	// ENGINE STATE:

	public bool IsDisposed { get; private set; } = false;
	/// <summary>
	/// Gets whether the engine is currently initialized and running. If true, one of the main loop states is likely active.
	/// </summary>
	public bool IsRunning
	{
		get => !IsDisposed && isRunning;
		private set => isRunning = !IsDisposed && value;
	}
	/// <summary>
	/// Gets whether the exit signal has been sent, and the engine is in the process of shutting down.
	/// </summary>
	public bool IsExiting => exitCancellationSource.IsCancellationRequested;
	/// <summary>
	/// Gets whether the engine is currently in a state that has a main loop.
	/// </summary>
	public bool IsInMainLoop => State.IsRunningMainLoop();

	/// <summary>
	/// Gets the current state of the engine's main statemachine.
	/// </summary>
	public EngineStateType State { get; private set; } = EngineStateType.None;

	// CORE SERVICES:

	public IServiceProvider Provider { get; }
	public ILogger Logger { get; }
	public PlatformService PlatformService { get; }
	public WindowService WindowService { get; }
	public GraphicsService Graphics { get; }
	//...

	#endregion
	#region Constructors

	public Engine(IServiceProvider? _serviceProvider = null)
	{
		// Initialize DI:
		if (_serviceProvider is null)
		{
			if (!EngineStartupHelper.CreateDefaultServiceCollection(out IServiceCollection? services))
			{
				throw new Exception("Failed to create default service collection!");
			}
			if (!EngineStartupHelper.CreateDefaultServiceProvider(services, out _serviceProvider))
			{
				throw new Exception("Failed to create default service provider!");
			}
		}

		Provider = _serviceProvider!;

		// Retrieve core services for easier access:
		Logger = Provider.GetService<ILogger>() ?? new ConsoleLogger();
		PlatformService = Provider.GetService<PlatformService>()!;
		WindowService = Provider.GetService<WindowService>()!;
		Graphics = Provider.GetService<GraphicsService>()!;
		//...

		// Create state machine states:
		startingState = new(this);
		loadingState = new(this);
		runningState = new(this);
		unloadingState = new(this);
		exitingState = new(this);

		currentState = startingState;
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
		engineStateSemaphore.Dispose();
	}

	/// <summary>
	/// Starts up the engine and processes the state machine. The main running state will loop and block until exit is requested.
	/// </summary>
	/// <returns>True if the engine started up, ran, and shut down without breaking issues.</returns>
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
		success &= SetState(EngineStateType.Starting);
		if (success)
		{
			success &= currentState!.Run(exitCancellationSource.Token);
			IsRunning = success;
		}

		// Load core content:
		if (success)
		{
			success = SetState(EngineStateType.Loading);
			if (success)
			{
				isLoaded = currentState!.Run(exitCancellationSource.Token);
				success &= isLoaded;
			}
		}

		// Run main application logic:
		if (isLoaded)
		{
			success &= SetState(EngineStateType.Running);
			if (success)
			{
				success &= currentState!.Run(exitCancellationSource.Token);
			}
		}

		// Unload all content:
		if (isLoaded)
		{
			success &= SetState(EngineStateType.Unloading);
			if (success)
			{
				success &= currentState!.Run(exitCancellationSource.Token);
			}
		}

		// Run shutdown logic:
		IsRunning = false;
		success &= SetState(EngineStateType.Exiting);
		if (success)
		{
			success &= currentState!.Run(CancellationToken.None);
		}

		return success;
	}

	/// <summary>
	/// Request the engine to end the main thread and quit.
	/// </summary>
	public void RequestExit()
	{
		if (IsDisposed || !IsRunning || IsExiting) return;

		// Cancel main loops and initiate shutdown procedure:
		engineStateSemaphore.Wait();
		exitCancellationSource.Cancel();
		engineStateSemaphore.Release();

		// Notify listeners that we're about to exit:
		ExitRequested?.Invoke();

		Logger.LogMessage("Exit has been requested.");
	}

	private bool SetState(EngineStateType _newState)
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
			if (State == EngineStateType.Loading || State == EngineStateType.Running)
			{
				_newState = EngineStateType.Unloading;
			}
			else if (State == EngineStateType.Starting)
			{
				_newState = EngineStateType.Exiting;
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

		if (!EndcurrentState(_newState))
		{
			Logger.LogError($"Failed to shutdown current engine state! (Current: '{State}', Target: '{_newState}')", LogEntrySeverity.Critical);
			return false;
		}

		// Change engine state:
		engineStateSemaphore.Wait();
		EngineStateType prevState = State;
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

	private bool EndcurrentState(EngineStateType _newState)
	{
		if (!IsExiting && _newState == EngineStateType.Exiting)
		{
			engineStateSemaphore.Wait();
			exitCancellationSource.Cancel();
			engineStateSemaphore.Release();
		}

		if (currentState is not null && !currentState.IsDisposed)
		{
			currentState.Shutdown();
		}

		return true;
	}

	private bool StartNewState()
	{
		if (State == EngineStateType.Starting)
		{
			engineStateSemaphore.Wait();
			exitCancellationSource = new();
			engineStateSemaphore.Release();
		}

		currentState = State switch
		{
			EngineStateType.Starting => startingState,
			EngineStateType.Loading => loadingState,
			EngineStateType.Running => runningState,
			EngineStateType.Unloading => unloadingState,
			EngineStateType.Exiting => exitingState,
			_ => null,
		};

		if (currentState is not null && !currentState.Initialize())
		{
			return false;
		}
		return true;
	}

	#endregion
}
