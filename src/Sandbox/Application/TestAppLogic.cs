using FragEngine.Application;
using FragEngine.EngineCore;
using FragEngine.EngineCore.Input.Keys;
using FragEngine.EngineCore.Windows;
using Veldrid;

namespace Sandbox.Application;

/// <summary>
/// Application logic for the sandbox test app.
/// </summary>
internal sealed class TestAppLogic : IAppLogic
{
	#region Fields

	private Engine engine = null!;

	private InputKeyState escapeKeyState = InputKeyState.Invalid;
	private InputKeyState fullscreenKeyState = InputKeyState.Invalid;

	#endregion
	#region Methods

	// LIFECYCLE:

	public bool Initialize(Engine _engine)
	{
		engine = _engine;
		return true;
	}

	public void Shutdown() { }

	// ENGINE STATEMACHINE:

	public bool OnEngineStateChanging(EngineStateType _currentState, EngineStateType _targetState)
	{
		return true;
	}

	public bool OnEngineStateChanged(EngineStateType _previousState, EngineStateType _currentState)
	{
		if (_currentState == EngineStateType.Running)
		{
			escapeKeyState = engine.InputService.GetKeyState(Key.Escape);
			fullscreenKeyState = engine.InputService.GetKeyState(Key.Tab);
		}

		return true;
	}

	// LOADING / UNLOADING:

	public bool UpdateLoadingState(out bool _loadingIsDone)
	{
		_loadingIsDone = true;
		return true;
	}

	public bool UpdateUnloadingState(out bool _outUnloadingIsDone)
	{
		_outUnloadingIsDone = true;
		return true;
	}

	// RUNNING:

	public bool UpdateRunningState_Draw()
	{
		return true;
	}

	public bool UpdateRunningState_Input()
	{
		if (escapeKeyState.EventType == InputKeyEventType.Released)
		{
			engine.RequestExit();
		}
		if (fullscreenKeyState.EventType == InputKeyEventType.Released && engine.Graphics.MainWindow is not null)
		{
			engine.Graphics.MainWindow.FillScreen(true);
		}

		return true;
	}

	public bool UpdateRunningState_Update()
	{
		return true;
	}

	#endregion
}
