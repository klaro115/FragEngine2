using FragEngine.Application;
using FragEngine.EngineCore;
using FragEngine.EngineCore.Input;
using FragEngine.EngineCore.Input.Keys;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics;
using FragEngine.Graphics.Cameras;
using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Contexts;
using FragEngine.Graphics.Geometry;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Scenes;
using Microsoft.Extensions.DependencyInjection;
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
	private InputKeyState switchMonitorKeyState = InputKeyState.Invalid;

	private CBScene cbSceneData = CBScene.Default;
	private CBObject cbObjectCube = CBObject.Default;

	private Camera? camera = null;
	private CommandList? cmdList = null;
	private DeviceBuffer? bufCbScene = null;
	private DeviceBuffer? bufCbObjectCube = null;

	private MeshSurface? cubeMesh = null;
	private Shader? shaderVertex = null;
	private Shader? shaderPixel = null;
	private ResourceLayout? cubeResLayout = null;
	private Pipeline? cubePipeline = null;

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
		if (!CameraHelper.CreatePerspectiveCamera(engine.Provider, out camera, _poseSource: new ConstantPoseSource(new(new(0, 1, -5))), _attachToWindowHandle: mainWindow))
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
		cubePipeline?.Dispose();
		cubeResLayout?.Dispose();
		cubeMesh?.Dispose();
		bufCbObjectCube?.Dispose();

		cubePipeline = null;
		cubeResLayout = null;
		cubeMesh = null;
		bufCbObjectCube = null;
	}

	private bool CreateScene()
	{
		PrimitivesFactory factory = engine.Provider.GetRequiredService<PrimitivesFactory>();
		GraphicsResourceService graphicsResources = engine.Provider.GetRequiredService<GraphicsResourceService>();
		GraphicsService graphicsService = engine.Graphics;

		if (!factory.CreateCubeMesh(Vector3.One, out cubeMesh, _createExtendedVertexData: false))
		{
			engine.Logger.LogError("Failed to create cube mesh!");
			return false;
		}

		if ((shaderVertex = graphicsResources.VSFallback) is null ||
			(shaderPixel = graphicsResources.PSFallback) is null)
		{
			engine.Logger.LogError("Failed to load fallback shaders!");
			return false;
		}

		ResourceLayoutDescription resLayoutDesc = new(
			CBGraphics.ResourceLayoutElementDesc,
			CBScene.ResourceLayoutElementDesc,
			CBCamera.ResourceLayoutElementDesc,
			CBObject.ResourceLayoutElementDesc);

		VertexLayoutDescription[] vertexLayoutDescs = cubeMesh.HasExtendedVertexData
			? [
				BasicVertex.LayoutDescription,
				ExtendedVertex.LayoutDescription,
			]
			: [
				BasicVertex.LayoutDescription,
			];

		ShaderSetDescription shaderDesc = new(
			vertexLayoutDescs,
			[
				shaderVertex,
				shaderPixel,
			]);

		try
		{
			cubeResLayout = graphicsService.ResourceFactory.CreateResourceLayout(ref resLayoutDesc);
			cubeResLayout.Name = "ResLayout_Cube";
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to create cube resource layout!", ex, LogEntrySeverity.Critical);
			return false;
		}

		GraphicsPipelineDescription pipelineDesc = new(
			BlendStateDescription.SingleOverrideBlend,
			DepthStencilStateDescription.DepthOnlyLessEqual,
			RasterizerStateDescription.Default,
			PrimitiveTopology.TriangleList,
			shaderDesc,
			cubeResLayout,
			camera!.OutputSettings.CreateBasicOutputDescription());

		try
		{
			cubePipeline = graphicsService.ResourceFactory.CreateGraphicsPipeline(ref pipelineDesc);
			cubePipeline.Name = "Pipeline_Cube";

			bufCbObjectCube ??= engine.Graphics.ResourceFactory.CreateBuffer(CBObject.BufferDesc);
			bufCbObjectCube.Name = $"{CBObject.resourceName}_Cube";
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to create cube pipeline and constant buffers!", ex, LogEntrySeverity.Critical);
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

		success &= DrawScene();

		camera.EndPass();
		camera.EndFrame();

		cmdList.End();

		if (success)
		{
			success &= engine.Graphics.CommitCommandList(cmdList);
		}

		return success;
	}

	private bool DrawScene()
	{
		if (cmdList is null || cubeMesh is null)
		{
			return true;
		}

		cmdList.UpdateBuffer(bufCbObjectCube, 0, ref cbObjectCube);

		cmdList.SetVertexBuffer(0, cubeMesh.BufVerticesBasic);
		if (cubeMesh.HasExtendedVertexData)
		{
			cmdList.SetVertexBuffer(1, cubeMesh.BufVerticesExt);
		}

		cmdList.SetIndexBuffer(cubeMesh.BufIndices, cubeMesh.IndexFormat);

		cmdList.SetPipeline(cubePipeline);

		cmdList.DrawIndexed((uint)cubeMesh.IndexCount);

		return true;
	}

	#endregion
}
