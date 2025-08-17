using FragEngine.Application;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Input;
using FragEngine.EngineCore.StateMachine;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Extensions;
using FragEngine.Graphics;
using FragEngine.Helpers;
using FragEngine.Interfaces;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

	// APPLICATION:

	private readonly IAppLogic appLogic;

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
	public TimeService TimeService { get; }
	public InputService InputService { get; }
	public WindowService WindowService { get; }
	public GraphicsService Graphics { get; }
	//...

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new engine instance.
	/// </summary>
	/// <param name="_appLogic">Application logic module. This is where your overarching app or game logic goes. May not be null.</param>
	/// <param name="_serviceCollection"></param>
	/// <exception cref="ArgumentNullException">Application logic was null.</exception>
	/// <exception cref="Exception">Failed to initialize depencency injection.</exception>
	/// <exception cref="ObjectDisposedException">Application logic has already been disposed.</exception>
	public Engine(IAppLogic _appLogic, IServiceCollection? _serviceCollection = null)
	{
		ArgumentNullException.ThrowIfNull(_appLogic);

		if (_appLogic is IExtendedDisposable appLogicDisposable && appLogicDisposable.IsDisposed)
		{
			throw new ObjectDisposedException(nameof(_appLogic), "Application logic has already been disposed!");
		}

		appLogic = _appLogic;

		// Initialize DI:
		if (!InitializeDependencyInjection(_serviceCollection, out IServiceProvider? serviceProvider))
		{
			throw new Exception("Failed to initialize dependency injection!");
		}
		Provider = serviceProvider!;

		// Retrieve core services for easier access:
		Logger = Provider.GetService<ILogger>() ?? new ConsoleLogger();
		PlatformService = Provider.GetService<PlatformService>()!;
		TimeService = Provider.GetService<TimeService>()!;
		InputService = Provider.GetService<InputService>()!;
		WindowService = Provider.GetService<WindowService>()!;
		Graphics = Provider.GetService<GraphicsService>()!;
		//...

		// Create state machine states:
		startingState = new(this, appLogic);
		loadingState = new(this, appLogic);
		runningState = new(this, appLogic);
		unloadingState = new(this, appLogic);
		exitingState = new(this, appLogic);

		currentState = startingState;

		// Initialize app logic:
		if (!appLogic.Initialize(this))
		{
			Dispose();
			throw new Exception("Failed to initialize engine's app logic!");
		}

		Logger.LogStatus("# Engine is initialized.");
	}

	~Engine()
	{
		if (!IsDisposed) Dispose(false);
	}

	private bool InitializeDependencyInjection(IServiceCollection? _serviceCollection, out IServiceProvider? _outServiceProvider)
	{	
		// If no customized service collection is provided, use a basic default setup:
		if (_serviceCollection is null && !EngineStartupHelper.CreateDefaultServiceCollection(out _serviceCollection!))
		{
			_outServiceProvider = null;
			return false;
		}

		// Ensure that a logging service is registered; if an implementation instance exists, use that for error logging:
		ILogger? diLogger;
		if (!_serviceCollection.HasService<ILogger>())
		{
			diLogger = new ConsoleLogger();
			_serviceCollection.AddSingleton(diLogger);
		}
		else if ((diLogger = _serviceCollection.GetImplementationInstance<ILogger>()) is null)
		{
			diLogger = new ConsoleLogger();
		}

		// Load or create the main engine config, and register it as a singleton:
		if (!_serviceCollection.HasService<EngineConfig>() && EngineStartupHelper.LoadEngineConfig(diLogger, out EngineConfig config))
		{
			_serviceCollection.AddSingleton(config);
		}
		else
		{
			config = _serviceCollection.GetImplementationInstance<EngineConfig>()!;
		}
		if (!config.IsValid())
		{
			diLogger.LogError("Cannot initialize dependency injection using invalid engine config!", LogEntrySeverity.Critical);
			_outServiceProvider = null;
			return false;
		}

		// If requested, add application logic as a service; remove it otherwise:
		if (config.Startup.AddAppLogicToServiceProvider && !_serviceCollection.HasService<IAppLogic>())
		{
			_serviceCollection.AddSingleton(appLogic);
		}
		else if (_serviceCollection.HasService<IAppLogic>())
		{
			_serviceCollection.RemoveAll<EngineConfig>();
		}

		// Add the engine itself as a singleton:
		_serviceCollection.AddSingleton(this);

		// Create the final service provider for the engine-wide DI:
		if (!EngineStartupHelper.CreateDefaultServiceProvider(_serviceCollection, out _outServiceProvider))
		{
			return false;
		}

		return true;
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
		else
		{
			Logger.LogWarning("Engine startup state has exited with errors!", LogEntrySeverity.High);
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
			if (!success || !isLoaded)
			{
				Logger.LogWarning("Engine loading state has exited with errors!", LogEntrySeverity.High);
			}
		}

		// Run main application logic:
		if (isLoaded)
		{
			bool isRunningMain = SetState(EngineStateType.Running);
			if (isRunningMain)
			{
				isRunningMain &= currentState!.Run(exitCancellationSource.Token);
			}
			success &= isRunningMain;
		}

		// Unload all content:
		if (isLoaded)
		{
			bool isUnloaded = SetState(EngineStateType.Unloading);
			if (isUnloaded)
			{
				isUnloaded &= currentState!.Run(exitCancellationSource.Token);
			}
			if (!isUnloaded)
			{
				Logger.LogWarning("Engine unloading state has exited with errors!", LogEntrySeverity.High);
			}
			success &= isUnloaded;
		}

		// Run shutdown logic:
		IsRunning = false;

		bool isExiting = SetState(EngineStateType.Exiting);
		if (isExiting)
		{
			isExiting &= currentState!.Run(CancellationToken.None);
		}
		if (!isExiting)
		{
			Logger.LogWarning("Engine exit state has ended with errors!", LogEntrySeverity.High);
		}
		success &= isExiting;

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
			EngineStateType originalTargetState = _newState;
			if (State == EngineStateType.Loading || State == EngineStateType.Running)
			{
				_newState = EngineStateType.Unloading;
			}
			else if (State == EngineStateType.Starting)
			{
				_newState = EngineStateType.Exiting;
			}

			if (_newState != originalTargetState)
			{
				Logger.LogWarning($"Exit has been requested, aborting state change to '{_newState}'.");
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

		// If requested, trigger GC before entering the new state:
		EngineConfig config = Provider.GetRequiredService<EngineConfig>();
		if (config.Optimizations.GarbageCollectIfStateChanged)
		{
			GC.Collect();
		}

		// Initialize new state:
		if (!StartNewState(prevState))
		{
			Logger.LogError($"Failed to initialize new engine state! (Current: '{State}')", LogEntrySeverity.Critical);
			return false;
		}

		return true;
	}

	private bool EndcurrentState(EngineStateType _newState)
	{
		// Warn the application:
		if (!appLogic.OnEngineStateChanging(State, _newState))
		{
			return false;
		}

		// Notify listeners that the state is about to change:
		StateChanging?.Invoke(State, _newState);

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

	private bool StartNewState(EngineStateType _prevState)
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

		Logger.LogStatus($"### ENGINE STATE CHANGED: {_prevState} => {State}");

		// Notify the application:
		if (!appLogic.OnEngineStateChanged(_prevState, State))
		{
			return false;
		}

		// Notify listeners that the state has changed:
		StateChanged?.Invoke(_prevState, State);

		return true;
	}

	#endregion
}
