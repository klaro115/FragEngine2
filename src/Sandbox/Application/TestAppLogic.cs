using FragEngine.Application;
using FragEngine.EngineCore;
using FragEngine.EngineCore.Input;
using FragEngine.EngineCore.Input.Keys;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics.Cameras;
using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Contexts;
using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Numerics;
using Veldrid;

namespace Sandbox.Application;

/// <summary>
/// Application logic for the sandbox test app.
/// </summary>
internal sealed class TestAppLogic : IAppLogic, IExtendedDisposable
{
	#region Fields

	private Engine engine = null!;

	private InputKeyState escapeKeyState = InputKeyState.Invalid;
	private InputKeyState fullscreenKeyState = InputKeyState.Invalid;

	private CBScene cbSceneData;

	private Camera? camera = null;
	private CommandList? cmdList = null;
	private DeviceBuffer? bufCbScene = null;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	~TestAppLogic()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	// LIFECYCLE:

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool _)
	{
		DisposeCamera();
	}

	public bool Initialize(Engine _engine)
	{
		engine = _engine;
		return true;
	}

	public void Shutdown()
	{
		DisposeCamera();
	}

	// ENGINE STATEMACHINE:

	public bool OnEngineStateChanging(EngineStateType _currentState, EngineStateType _targetState)
	{
		return true;
	}

	public bool OnEngineStateChanged(EngineStateType _previousState, EngineStateType _currentState)
	{
		if (_currentState == EngineStateType.Starting)
		{
			engine.InputService.AddInputAxesWASDQE();
		}
		
		if (_currentState == EngineStateType.Loading)
		{
			escapeKeyState = engine.InputService.GetKeyState(Key.Escape);
			fullscreenKeyState = engine.InputService.GetKeyState(Key.Tab);

			if (!CreateCamera())
			{
				DisposeCamera();
			}
		}
		
		if (_currentState >= EngineStateType.Unloading)
		{
			DisposeCamera();
		}

		return true;
	}

	// LOADING / UNLOADING:

	public bool UpdateLoadingState(out bool _loadingIsDone)
	{
		bool success = true;

		if (camera is not null)
		{
			success &= DrawCamera();
		}

		_loadingIsDone = true;
		return success;
	}

	public bool UpdateUnloadingState(out bool _outUnloadingIsDone)
	{
		_outUnloadingIsDone = true;
		return true;
	}

	// RUNNING:

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

	public bool UpdateRunningState_Draw()
	{
		bool success = true;

		if (camera is not null)
		{
			success &= DrawCamera();
		}

		return success;
	}

	#endregion
	#region Methods Logic

	private void DisposeCamera()
	{
		camera?.Dispose();
		cmdList?.Dispose();
		bufCbScene?.Dispose();

		camera = null;
		cmdList = null;
		bufCbScene = null;
	}

	private bool CreateCamera()
	{
		if (!GetMainWindow(out WindowHandle? mainWindow))
		{
			engine.Logger.LogError("Engine does not have a live main window!!");
			return false;
		}
		mainWindow!.IsResizable = true;

		// Create camera instance:
		try
		{
			camera = new(engine.Graphics, engine.Logger)
			{
				CurrentPose = new(new(0, 1, -5), Quaternion.Identity, Vector3.One),
			};
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to create main camera!", ex, LogEntrySeverity.Critical);
			return false;
		}
		
		if (!mainWindow.ConnectClient(camera))
		{
			return false;
		}

		// Create various scene-wide resources:
		try
		{
			bufCbScene ??= engine.Graphics.ResourceFactory.CreateBuffer(CBScene.BufferDesc);
			cmdList ??= engine.Graphics.ResourceFactory.CreateCommandList();
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to create scene graphics resources!", ex, LogEntrySeverity.Critical);
			return false;
		}

		return true;
	}

	private bool GetMainWindow(out WindowHandle? _outMainWindow)
	{
		if (engine.WindowService.WindowCount == 0)
		{
			_outMainWindow = null;
			return false;
		}

		return engine.WindowService.GetWindow(0, out _outMainWindow);
	}

	private bool DrawCamera()
	{
		cmdList!.Begin();

		if (!engine.Graphics.BeginFrame(cmdList, out GraphicsContext? graphicsCtx))
		{
			cmdList.End();
			return false;
		}

		SceneContext sceneCtx = new(graphicsCtx)
		{
			CbScene = cbSceneData,
			BufCbScene = bufCbScene!,
		};

		bool success = true;


		cmdList.UpdateBuffer(bufCbScene, 0, ref cbSceneData);

		if (!camera!.BeginFrame(in sceneCtx, 0u))
		{
			cmdList.End();
			return false;
		}

		success &= camera.BeginPass(in sceneCtx, cmdList!, 0u, out CameraPassContext? _);


		//TODO [later]: Draw stuff.


		camera.EndPass();
		camera.EndFrame();

		cmdList.End();

		if (success)
		{
			success &= engine.Graphics.CommitCommandList(cmdList);
		}

		return success;
	}

	#endregion
}
