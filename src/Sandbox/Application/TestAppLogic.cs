using FragEngine.Application;
using FragEngine.EngineCore;
using FragEngine.EngineCore.Input;
using FragEngine.EngineCore.Input.Axes;
using FragEngine.EngineCore.Input.Keys;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics;
using FragEngine.Graphics.Cameras;
using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Contexts;
using FragEngine.Graphics.Geometry;
using FragEngine.Graphics.Renderers;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources;
using FragEngine.Scenes;
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

	// INPUT:

	private InputKeyState escapeKeyState = InputKeyState.Invalid;
	private InputKeyState fullscreenKeyState = InputKeyState.Invalid;
	private InputKeyState switchMonitorKeyState = InputKeyState.Invalid;
	private InputKeyState resetCameraKeyState = InputKeyState.Invalid;

	private KeyboardAxis axisHorizontal = (InputAxis.Invalid as KeyboardAxis)!;
	private KeyboardAxis axisVertical = (InputAxis.Invalid as KeyboardAxis)!;
	private InputKeyState testKeyState = InputKeyState.Invalid;

	// SCENE & CAMERA:

	private CBScene cbSceneData = CBScene.Default;

	private Camera? camera = null;
	private CommandList? cmdList = null;
	private DeviceBuffer? bufCbScene = null;

	private SimpleMeshRenderer? cubeRenderer = null;

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
		DisposeScene();
	}

	public bool Initialize(Engine _engine)
	{
		engine = _engine;
		return true;
	}

	public void Shutdown()
	{
		DisposeCamera();
		DisposeScene();
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
		else if (_currentState == EngineStateType.Loading)
		{
			escapeKeyState = engine.InputService.GetKeyState(Key.Escape);
			fullscreenKeyState = engine.InputService.GetKeyState(Key.Tab);
			switchMonitorKeyState = engine.InputService.GetKeyState(Key.KeypadMultiply);
			resetCameraKeyState = engine.InputService.GetKeyState(Key.Number0);

			axisHorizontal = (engine.InputService.GetInputAxis(InputServiceExt.AxisNameAD) as KeyboardAxis)!;
			axisVertical = (engine.InputService.GetInputAxis(InputServiceExt.AxisNameSW) as KeyboardAxis)!;
			axisHorizontal.ValuesAreDiscrete = false;
			axisHorizontal.InterpolationRate = 1;

			testKeyState = engine.InputService.GetKeyState(Key.D);

			if (!CreateCamera())
			{
				DisposeCamera();
			}
		}
		else if (_currentState == EngineStateType.Running)
		{
			if (!CreateScene())
			{
				DisposeScene();
			}
		}
		else if (_currentState >= EngineStateType.Unloading)
		{
			DisposeCamera();
			DisposeScene();
		}

		return true;
	}

	// LOADING / UNLOADING:

	public bool UpdateLoadingState(bool _hasDataScanCompleted, out bool _loadingIsDone)
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
		WindowHandle? mainWindow = engine.Graphics.MainWindow;

		if (escapeKeyState.EventType == InputKeyEventType.Released)
		{
			engine.RequestExit();
		}
		if (fullscreenKeyState.EventType == InputKeyEventType.Released && mainWindow is not null)
		{
			mainWindow.FillScreen(true);
		}
		if (switchMonitorKeyState.EventType == InputKeyEventType.Released && mainWindow is not null)
		{
			engine.WindowService.GetScreenCount(out int screenCount);
			mainWindow.GetScreenIndex(out int screenIdx);
			screenIdx = (screenIdx + 1) % screenCount;
			mainWindow.MoveToScreen(screenIdx);
		}
		if (resetCameraKeyState.EventType == InputKeyEventType.Released && camera is not null)
		{
			camera.MarkDirty();
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
			engine.Logger.LogError("Engine does not have a live main window!");
			return false;
		}
		mainWindow!.IsResizable = true;

		// Create camera instance:
		//if (!CameraHelper.CreatePerspectiveCamera(engine.Provider, out camera, _poseSource: new ConstantPoseSource(new(new(0, 1, -5))), _attachToWindowHandle: mainWindow))
		if (!CameraHelper.CreateOrthographicsCamera(engine.Provider, out camera, _poseSource: new ConstantPoseSource(new(new(0, 0, -1))), _attachToWindowHandle: mainWindow))
		{
			engine.Logger.LogError("Failed to create main camera!", LogEntrySeverity.Critical);
			return false;
		}

		// Create various scene-wide resources:
		try
		{
			bufCbScene ??= engine.Graphics.ResourceFactory.CreateBuffer(CBScene.BufferDesc);
			bufCbScene.Name = CBScene.resourceName;

			cmdList ??= engine.Graphics.ResourceFactory.CreateCommandList();
			cmdList.Name = "CmdList";
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to create scene graphics resources!", ex, LogEntrySeverity.Critical);
			return false;
		}

		return true;
	}

	private void DisposeScene()
	{
		cubeRenderer?.Dispose();
		cubeRenderer = null;
	}

	private bool CreateScene()
	{
		//PrimitivesFactory factory = engine.Provider.GetRequiredService<PrimitivesFactory>();
		GraphicsService graphicsService = engine.Graphics;
		ResourceService resourceService = engine.Resources;

		// Create geometry:
		MeshSurfaceData testData = new();
		testData.SetVertices(
			[
				new(new(0, 0, 0), -Vector3.UnitZ, new(0, 0)),
				new(new(1, 0, 0), -Vector3.UnitZ, new(1, 0)),
				new(new(0, 1, 0), -Vector3.UnitZ, new(0, 1)),
			],
			[
				new(Vector3.UnitY, Vector3.UnitX, new(0, 0)),
				new(Vector3.UnitY, Vector3.UnitX, new(1, 0)),
				new(Vector3.UnitY, Vector3.UnitX, new(0, 1)),
			],
			3,
			engine.Logger);
		testData.SetIndices16(
			[
				0, 1, 2,
			],
			3,
			IndexFormat.UInt16,
			engine.Logger);

		MeshSurface cubeMesh = new(engine.Graphics, engine.Logger);
		cubeMesh.SetData(in testData);

		/*
		if (!factory.CreateCubeMesh(Vector3.One, out cubeMesh, _createExtendedVertexData: false))
		{
			engine.Logger.LogError("Failed to create cube mesh!");
			return false;
		}
		*/

		try
		{
			cubeRenderer = new(graphicsService, resourceService, engine.Logger)
			{
				DontDrawUntilFullyLoaded = true,
				Mesh = cubeMesh,
				VertexShaderKey = "VS_Basic",
				PixelShaderKey = "PS_Fallback",
			};
			return true;
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to create cube renderer!", ex);
			return false;
		}
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

		//TEST
		CameraClearingSettings clearSettings = new()
		{
			ClearColorTargets = CameraClearingFlags.EachFrame,
			ClearDepthBuffer = CameraClearingFlags.EachFrame,
			ClearStencilBuffer = CameraClearingFlags.Never,
			
			ColorValues =
			[
				new(Math.Max(axisHorizontal.CurrentValue, 0),
					Math.Max(-axisHorizontal.CurrentValue, 0),
					1,
					1),
			],
			DepthValue = 1,
			StencilValue = 0,
		};
		camera!.SetClearingSettings(clearSettings);
		//Console.WriteLine(camera.ClearingSettings.ColorValues[0]);
		//TEST

		if (!engine.Graphics.BeginFrame(cmdList, out GraphicsContext? graphicsCtx))
		{
			cmdList.End();
			return false;
		}

		ulong sceneChecksum =
			graphicsCtx.Checksum ^
			(ulong)Math.Abs(bufCbScene!.GetHashCode());

		SceneContext sceneCtx = new(graphicsCtx)
		{
			Checksum = sceneChecksum,

			CbScene = cbSceneData,
			BufCbScene = bufCbScene,
			//...
		};

		bool success = true;

		cmdList.UpdateBuffer(bufCbScene, 0, ref cbSceneData);

		if (!camera!.BeginFrame(in sceneCtx, 0u))
		{
			cmdList.End();
			return false;
		}

		success &= camera.BeginPass(in sceneCtx, cmdList!, 0u, out CameraPassContext? cameraCtx);

		if (success)
		{
			success &= DrawScene(in cameraCtx!);
		}

		camera.EndPass();
		camera.EndFrame();

		cmdList.End();

		if (success)
		{
			success &= engine.Graphics.CommitCommandList(cmdList);
		}

		return success;
	}

	private bool DrawScene(in CameraPassContext _cameraCtx)
	{
		if (cmdList is null || cubeRenderer is null)
		{
			return true;
		}

		bool success = cubeRenderer.Draw(in _cameraCtx);
		return success;
	}

	#endregion
}
