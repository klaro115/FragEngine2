using FragEngine.Application;
using FragEngine.EngineCore;
using FragEngine.EngineCore.Input;
using FragEngine.EngineCore.Input.Keys;
using Veldrid;

namespace Sandbox.Application;

/// <summary>
/// Application logic for the sandbox test app.
/// </summary>
internal sealed class TestAppLogic : IAppLogic
{
	#region Fields

	private Engine engine = null!;

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

	public bool OnEngineStateChanged(EngineStateType _previousState, EngineStateType _currentState)
	{
		return true;
	}

	public bool OnEngineStateChanging(EngineStateType _currentState, EngineStateType _targetState)
	{
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
		InputKeyState escapeState = engine.InputService.GetKeyState(Key.Escape)!;
		if (escapeState.EventType == InputKeyEventType.Released)
		{
			engine.RequestExit();
		}

		return true;
	}

	public bool UpdateRunningState_Update()
	{
		return true;
	}

	#endregion
}
