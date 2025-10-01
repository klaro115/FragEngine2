using FragEngine.EngineCore.Windows;
using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Contexts;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Scenes;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// A camera for rendering a 3D scene. The camera handles most of the resource management, logistics, and math that need to happen
/// before a frame can be rendered.
/// <para/>
/// USAGE: In order to draw geometry using a camera, initialize your scene-wide graphics resources (<see cref="SceneContext"/>),
/// then start camera logic by calling '<see cref="BeginFrame(in SceneContext, uint)"/>', followed by one or more calls to
/// '<see cref="BeginPass(in SceneContext, CommandList, uint, out CameraPassContext?)"/>'. Camera passes allow you to group or
/// isolate the creation of draw calls that can either be parallelized, or that use different rendering techniques. Each camera
/// pass must be ended by calling '<see cref="EndPass"/>'. After the final pass of a frame, the frame itself can be ended by calling
/// '<see cref="EndFrame"/>'.
/// <para/>
/// POSING: The camera has a physical location and orientation in space, which is sourced either from an '<see cref="IPoseSource"/>',
/// or by setting a pose by-value through the '<see cref="CurrentPose"/>' property. Use these to position a camera within a 3D scene.
/// The current pose defines the camera's local space for all subsequent projection math.
/// <para/>
/// PROJECTION: This camera class is capable of both perpective and orthographic projection. You can access and adjust projection
/// behaviour through the '<see cref="ProjectionSettings"/>' property.
/// </summary>
public sealed class Camera : IExtendedDisposable, IWindowClient
{
	#region Types

	private sealed class ConstantPoseSource(Pose _initialPose) : IPoseSource
	{
		public Pose currentPose = _initialPose;

		public Pose GetLocalPose() => currentPose;
		public Pose GetWorldPose() => currentPose;
	}

	#endregion
	#region Events

	/// <summary>
	/// Event that is triggered when the camera starts drawing a new frame.
	/// </summary>
	public event FuncCameraFrameStarted? FrameStarted = null;
	/// <summary>
	/// Event that is triggered when the camera finishes drawing a frame.
	/// </summary>
	public event FuncCameraFrameEnded? FrameEnded = null;

	/// <summary>
	/// Event that is triggered when the camera's '<see cref="OutputSettings"/>' are updated.
	/// </summary>
	public event FuncCameraOutputSettingsChanged? OutputSettingsChanged = null;

	/// <summary>
	/// Event that is triggered when the camera's '<see cref="ProjectionSettings"/>' are updated.
	/// </summary>
	public event FuncCameraProjectionSettingsChanged? ProjectionSettingsChanged = null;

	/// <summary>
	/// Event that is triggered when the camera's '<see cref="ClearingSettings"/>' are updated.
	/// </summary>
	public event FuncCameraClearingSettingsChanged? ClearingSettingsChanged = null;

	/// <summary>
	/// Event that is triggered when the camera's '<see cref="OverrideTarget"/>' changes or is unassigmed.
	/// </summary>
	public event FuncCameraOverrideTargetChanged? OverrideTargetChanged = null;

	#endregion
	#region Fields

	private readonly GraphicsService graphicsService;
	private readonly ILogger logger;

	private bool isDrawingFrame = false;
	private bool isDrawingPass = false;
	private uint currentFrameIndex = 0u;

	private IPoseSource currentPoseSource;

	private CBCamera cbCameraData = CBCamera.Default;
	private readonly DeviceBuffer bufCbCamera;

	private CameraTargets? ownTarget = null;

	private ulong projectionChecksum = CameraProjectionSettings.UNINITIALIZED_CHECKSUM;
	private ulong ownTargetChecksum = CameraOutputSettings.UNINITIALIZED_CHECKSUM;

	private static int initializedCameraCount = 0;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets whether the camera is currently being used to draw a frame.
	/// </summary>
	public bool IsDrawingFrame
	{
		get => !IsDisposed && isDrawingFrame;
		private set => isDrawingFrame = value && !IsDisposed;
	}

	/// <summary>
	/// Gets whether the camera is currently being used to draw a render pass.
	/// </summary>
	public bool IsDrawingPass
	{
		get => IsDrawingFrame && isDrawingPass;
		private set => isDrawingPass = value && IsDrawingFrame;
	}

	/// <summary>
	/// Gets or sets the current pose of the camera in world space.
	/// </summary>
	public Pose CurrentPose
	{
		get => currentPoseSource.GetWorldPose();
		set
		{
			if (currentPoseSource is ConstantPoseSource constantSource)
			{
				constantSource.currentPose = value;
			}
			else
			{
				currentPoseSource = new ConstantPoseSource(value);
			}
		}
	}

	/// <summary>
	/// Gets or sets a source from which the current pose in world space can be retrieved.
	/// </summary>
	public IPoseSource CurrentPoseSource
	{
		get => currentPoseSource;
		set => currentPoseSource = value ?? new ConstantPoseSource(CurrentPose);
	}

	/// <summary>
	/// Gets the camera's current output settings.<para/>
	/// Output settings may be changed in-between frames using '<see cref="SetOutputSettings(CameraOutputSettings)"/>'.
	/// </summary>
	public CameraOutputSettings OutputSettings { get; private set; } = CameraOutputSettings.Default;

	/// <summary>
	/// Gets the camera's current projection settings.<para/>
	/// Projection settings may be changed in-between passes using '<see cref="SetProjectionSettings(CameraProjectionSettings)"/>'.
	/// </summary>
	public CameraProjectionSettings ProjectionSettings { get; private set; } = CameraProjectionSettings.Default;

	/// <summary>
	/// Gets the camera's current settings for clearing render targets.<para/>
	/// Clearing settings may be changed in-between frames using '<see cref="SetClearingSettings(CameraClearingSettings)"/>'.
	/// </summary>
	public CameraClearingSettings ClearingSettings { get; private set; } = CameraClearingSettings.Default;

	/// <summary>
	/// Gets the camera's current target override. If this is non-null, the camera will render to this target instead
	/// of its own internal target.<para/>
	/// Override camera targets may be changed in-between passes using '<see cref="SetOverrideCameraTarget(CameraTargets?)"/>'.
	/// </summary>
	public CameraTargets? OverrideTarget { get; private set; } = null;

	/// <summary>
	/// Gets the camera's current render target. This will be camera's internal render targets, unless an '<see cref="OverrideTarget"/>'
	/// was assigned. If there is no override and no internal target is yet initialized, this may be null.
	/// </summary>
	public CameraTargets? CurrentTarget => OverrideTarget is not null && !OverrideTarget.IsDisposed ? OverrideTarget : ownTarget;

	/// <summary>
	/// Gets the window that this camera is currently connected to, or null, if it is not currently connected to
	/// any windows. If non-null, the camera will be outputting directly to this window's backbuffer, and its
	/// render targets will automatically resize to match the window's resolution.<para/>
	/// To bind the camera to a window, call '<see cref="WindowHandle.ConnectClient(IWindowClient)"/>', or use
	/// '<see cref="WindowHandle.DisconnectClient(IWindowClient)"/>' to detach it once more. The camera can only
	/// ever be connected to one window at a time.
	/// </summary>
	public WindowHandle? ConnectedWindow { get; private set; } = null;

	/// <summary>
	/// Gets the current number of cameras that are initialized and have not been disposed.
	/// This includes all cameras instances across the entire application, not just for this engine instance.
	/// </summary>
	public static int InitializedCameraCount => initializedCameraCount;

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new camera.
	/// </summary>
	/// <param name="_graphicsService">The graphics service singleton.</param>
	/// <param name="_logger">The logging service singleton.</param>
	/// <exception cref="ArgumentNullException">Graphics service or logger were null.</exception>
	/// <exception cref="Exception">Failure to create graphics resources.</exception>
	public Camera(GraphicsService _graphicsService, ILogger _logger)
	{
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ArgumentNullException.ThrowIfNull(_logger);

		graphicsService = _graphicsService;
		logger = _logger;

		currentPoseSource = new ConstantPoseSource(Pose.Identity);

		try
		{
			bufCbCamera = graphicsService.ResourceFactory.CreateBuffer(CBCamera.BufferDesc);
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to create constant buffer '{nameof(CBCamera)}'!", ex);
		}

		Interlocked.Increment(ref initializedCameraCount);
	}

	~Camera()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool _isDisposing)
	{
		if (_isDisposing && IsDrawingFrame)
		{
			EndFrame();
		}

		if (!IsDisposed)
		{
			Interlocked.Decrement(ref initializedCameraCount);
		}

		IsDisposed = true;

		bufCbCamera?.Dispose();
		ownTarget?.Dispose();

	}

	/// <summary>
	/// Marks the camera's internal state as dirty, which will cause it to be reset or reconstructed before next use.
	/// </summary>
	public void MarkDirty()
	{
		projectionChecksum = CameraProjectionSettings.UNINITIALIZED_CHECKSUM;
		ownTargetChecksum = CameraOutputSettings.UNINITIALIZED_CHECKSUM;
	}

	/// <summary>
	/// Begins a new frame with this camera. This will (re)allocate internal resources and clear render targets.
	/// </summary>
	/// <remarks>
	/// Each camera frame starts with a call to '<see cref="BeginFrame(in SceneContext, uint)"/>', and ends with a
	/// call to '<see cref="EndFrame"/>'. Within a frame, one or more camera passes can take place, each starting
	/// with a call to '<see cref="BeginPass(in SceneContext, CommandList, uint, out CameraPassContext?)"/>', and
	/// ending with a call to '<see cref="EndPass"/>'. Draw calls to render objects may happen within a camera pass.
	/// </remarks>
	/// <param name="_sceneCtx">A context object with scene-wide GPU resources and settings.</param>
	/// <param name="_cameraIndex">The index of this camera. This index may be used to map camera-specific resources
	/// and is exposed to shaders via the pass-specific '<see cref="CBCamera"/>' constant buffer.</param>
	/// <returns>True if a new frame was started successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Scene context may not be null.</exception>
	public bool BeginFrame(in SceneContext _sceneCtx, uint _cameraIndex)
	{
		ArgumentNullException.ThrowIfNull(_sceneCtx);
		
		if (IsDisposed)
		{
			logger.LogError("Cannot begin frame using disposed camera!");
			return false;
		}
		if (IsDrawingFrame)
		{
			logger.LogError("Cannot begin frame using camera that is already drawing!");
			return false;
		}
		if (!_sceneCtx.IsValid())
		{
			logger.LogError("Cannot begin frame using invalid scene context!");
			return false;
		}

		// Recalculate projections and matrices:
		CameraHelper.CalculateProjectionMatrices(
			OutputSettings,
			ProjectionSettings,
			CurrentPose,
			out Matrix4x4 mtxWorld2Clip,
			out Matrix4x4 mtxClip2World);

		projectionChecksum = ProjectionSettings.Checksum;

		// Update constant buffer data:
		{
			Vector4 backgroundColor = ClearingSettings.ClearColorTargets != CameraClearingFlags.Never
				? ClearingSettings.ColorValues[0].ToVector4()
				: Vector4.Zero;

			cbCameraData.mtxWorld2Clip = mtxWorld2Clip;
			cbCameraData.mtxClip2World = mtxClip2World;
			cbCameraData.backgroundColor = backgroundColor;
			cbCameraData.cameraIndex = _cameraIndex;
			cbCameraData.cameraPassIndex = 0u;
			cbCameraData.resolutionX = OutputSettings.ResolutionX;
			cbCameraData.resolutionY = OutputSettings.ResolutionY;
			cbCameraData.nearClipPlane = ProjectionSettings.NearClipPlane;
			cbCameraData.farClipPlane = ProjectionSettings.FarClipPlane;
		}

		// Check if any previously assigned override targets are still alive:
		if (OverrideTarget is not null && OverrideTarget.IsDisposed)
		{
			logger.LogWarning("Override camera target was disposed; unassigning and falling back to internal targets.");

			if (!SetOverrideCameraTarget_internal(null))
			{
				return false;
			}
		}

		// If using internal render targets, ensure those are initialized:
		if (OverrideTarget is null && !CheckOrRecreateOwnCameraTarget())
		{
			logger.LogError("Failed to (re)create internal camera target!");
			return false;
		}

		// Raise drawing flags:
		IsDrawingFrame = true;
		currentFrameIndex = _sceneCtx.GraphicsCtx.CbGraphics.frameIndex;

		FrameStarted?.Invoke(this, _cameraIndex, currentFrameIndex);
		return true;
	}

	/// <summary>
	/// Ends an ongoing frame.
	/// </summary>
	/// <remarks>
	/// Ending the frame will also end any ongoing camera passes that have not been via '<see cref="EndPass"/>' yet.
	/// </remarks>
	public void EndFrame()
	{
		// End any ongoing camera passes:
		if (IsDrawingPass)
		{
			EndPass();
		}

		IsDrawingFrame = false;

		FrameEnded?.Invoke(this, cbCameraData.cameraIndex, currentFrameIndex);
	}

	public bool BeginPass(in SceneContext _sceneCtx, CommandList _cmdList, uint _passIndex, [NotNullWhen(true)] out CameraPassContext? _outCameraPassCtx)
	{
		ArgumentNullException.ThrowIfNull(_sceneCtx);
		ArgumentNullException.ThrowIfNull(_cmdList);
		ObjectDisposedException.ThrowIf(_cmdList.IsDisposed, _cmdList);

		if (!IsDrawingFrame)
		{
			_outCameraPassCtx = null;
			return false;
		}
		if (IsDrawingPass)
		{
			_outCameraPassCtx = null;
			return false;
		}

		IsDrawingPass = true;

		// If changed, recalculate projections and matrices:
		if (projectionChecksum != ProjectionSettings.Checksum)
		{
			CameraHelper.CalculateProjectionMatrices(
				OutputSettings,
				ProjectionSettings,
				CurrentPose,
				out cbCameraData.mtxWorld2Clip,
				out cbCameraData.mtxClip2World);

			projectionChecksum = ProjectionSettings.Checksum;
		}

		// Bind camera target's framebuffer to pipeline:
		_cmdList.SetFramebuffer(CurrentTarget!.Framebuffer);

		// Update constant buffer data:
		{
			cbCameraData.cameraPassIndex = _passIndex;
		}

		// Upload CBCamera to GPU buffer:
		_cmdList.UpdateBuffer(bufCbCamera, 0u, cbCameraData);

		// If requested, clear render targets:
		CheckAndClearRenderTargets(_cmdList, _passIndex);

		// Assemble camera pass context:
		_outCameraPassCtx = new(_sceneCtx)
		{
			CmdList = _cmdList,
			CbCamera = cbCameraData,
			BufCbCamera = bufCbCamera!,
			//...
		};

		return true;
	}

	public void EndPass()
	{
		IsDrawingPass = false;
	}

	/// <summary>
	/// Try to update the camera's output settings.
	/// </summary>
	/// <remarks>
	/// Settings can only be changed while '<see cref="IsDrawingFrame"/>' is false. Any changes to render targets'
	/// pixel formats or resolution must happen in-between frames, and can never be done during an ongoing frame.
	/// </remarks>
	/// <param name="_newOutputSettings">The new output settings.</param>
	/// <returns>True if the output settings were updated, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">New output settings may not be null.</exception>
	public bool SetOutputSettings(CameraOutputSettings _newOutputSettings)
	{
		ArgumentNullException.ThrowIfNull(_newOutputSettings);

		if (IsDisposed)
		{
			logger.LogError("Cannot set output settings on disposed camera!");
			return false;
		}
		if (IsDrawingFrame)
		{
			logger.LogError("Cannot change output settings on camera while it is drawing a frame!");
			return false;
		}
		if (!_newOutputSettings.IsValid())
		{
			logger.LogError($"Cannot set camera output settings, invalid settings! (Settings: {_newOutputSettings})");
			return false;
		}

		// Only replace settings if values (and checksum) have changed:
		if (_newOutputSettings.Checksum == OutputSettings.Checksum)
		{
			return true;
		}

		// Output settings have changed, purge internal render targets:
		ownTarget?.Dispose();
		ownTargetChecksum = CameraOutputSettings.UNINITIALIZED_CHECKSUM;

		// Adopt new settings:
		CameraOutputSettings prevOutputSettings = OutputSettings;
		OutputSettings = _newOutputSettings;

		OutputSettingsChanged?.Invoke(this, prevOutputSettings, OutputSettings);
		return true;
	}

	/// <summary>
	/// Try to update the camera's projection settings.
	/// </summary>
	/// <remarks>
	/// Settings can only be changed while '<see cref="IsDrawingPass"/>' is false. Any changes to render targets'
	/// pixel formats or resolution must happen in-between camera passes, and can never be done during an ongoing pass.
	/// </remarks>
	/// <param name="_newProjectionSettings">The new projection settings.</param>
	/// <returns>True if the projection settings were updated, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">New projection settings may not be null.</exception>
	public bool SetProjectionSettings(CameraProjectionSettings _newProjectionSettings)
	{
		ArgumentNullException.ThrowIfNull(_newProjectionSettings);

		if (IsDisposed)
		{
			logger.LogError("Cannot set projection settings on disposed camera!");
			return false;
		}
		if (IsDrawingPass)
		{
			logger.LogError("Cannot change projection settings on camera while it is drawing a pass!");
			return false;
		}
		if (!_newProjectionSettings.IsValid())
		{
			logger.LogError($"Cannot set camera projection settings, invalid settings! (Settings: {_newProjectionSettings})");
			return false;
		}

		// Only replace settings if values (and checksum) have changed:
		if (_newProjectionSettings.Checksum == ProjectionSettings.Checksum)
		{
			return true;
		}

		// Adopt new settings:
		CameraProjectionSettings prevProjectionSettings = ProjectionSettings;
		ProjectionSettings = _newProjectionSettings;

		ProjectionSettingsChanged?.Invoke(this, prevProjectionSettings, ProjectionSettings);
		return true;
	}

	/// <summary>
	/// Try to update the camera's projection settings.
	/// </summary>
	/// <remarks>
	/// Settings can only be changed while '<see cref="IsDrawingFrame"/>' is false. Any changes to render targets'
	/// clearing behaviour must happen in-between frames, and can never be done during an ongoing frame.
	/// </remarks>
	/// <param name="_newClearingSettings">The new clearing settings.</param>
	/// <returns>True if the clearing settings were updated, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">New clearing settings may not be null.</exception>
	public bool SetClearingSettings(CameraClearingSettings _newClearingSettings)
	{
		ArgumentNullException.ThrowIfNull(_newClearingSettings);

		if (IsDisposed)
		{
			logger.LogError("Cannot set clearing settings on disposed camera!");
			return false;
		}
		if (IsDrawingPass)
		{
			logger.LogError("Cannot change clearing settings on camera while it is drawing a frame!");
			return false;
		}
		if (!_newClearingSettings.IsValid())
		{
			logger.LogError($"Cannot set camera clearing settings, invalid settings! (Settings: {_newClearingSettings})");
			return false;
		}

		// Only replace settings if values (and checksum) have changed:
		if (_newClearingSettings.Checksum == ClearingSettings.Checksum)
		{
			return true;
		}

		// Adopt new settings:
		CameraClearingSettings prevClearingSettings = ClearingSettings;
		ClearingSettings = _newClearingSettings;

		ClearingSettingsChanged?.Invoke(this, prevClearingSettings, ClearingSettings);
		return true;
	}

	/// <summary>
	/// Try to assign or unassign an override camera target.
	/// </summary>
	/// <remarks>
	/// Override targets can only be changed while '<see cref="IsDrawingPass"/>' is false. Any changes to camera targets
	/// must happen in-between camera passes, and can never be done during an ongoing pass.
	/// </remarks>
	/// <param name="_newOverrideTarget">The new override for the camera's render target. If null, the current
	/// override will be unassigned and the camera's internal targets will be used instead.<para/>
	/// Note that override targets must be compatible with the camera's '<see cref="OutputSettings"/>'; use
	/// '<see cref="CameraTargets.IsCompatibleWithOutput(in CameraOutputSettings?)"/>' to check compatibility.</param>
	/// <returns>True if the override target was set, false otherwise.</returns>
	public bool SetOverrideCameraTarget(CameraTargets? _newOverrideTarget)
	{
		if (ConnectedWindow is not null && ConnectedWindow.IsOpen)
		{
			logger.LogError("Cannot change override target on camera whose output is connected to a window!");
			return false;
		}

		return SetOverrideCameraTarget_internal(_newOverrideTarget);
	}

	private bool SetOverrideCameraTarget_internal(CameraTargets? _newOverrideTarget)
	{
		if (IsDisposed)
		{
			logger.LogError("Cannot set override target on disposed camera!");
			return false;
		}
		if (IsDrawingPass)
		{
			logger.LogError("Cannot change override target on camera while it is drawing a pass!");
			return false;
		}

		CameraTargets? prevOverrideTarget = OverrideTarget;

		// If null, unassign override target and use internal targets instead:
		if (_newOverrideTarget is null)
		{
			OverrideTarget = null;

			OverrideTargetChanged?.Invoke(this, prevOverrideTarget, null);
			return true;
		}

		// Check if new target is valid and compatible with current camera settings:
		if (_newOverrideTarget.IsDisposed || !_newOverrideTarget.IsValid())
		{
			logger.LogError($"Cannot set camera override target, invalid or disposed target! (New target: {_newOverrideTarget})");
			return false;
		}
		if (!_newOverrideTarget.IsCompatibleWithOutput(OutputSettings))
		{
			logger.LogError($"Cannot set camera override target, new target is not compatible with output settings! (Settings: {OutputSettings})");
			return false;
		}

		// Do nothing if the same target is already assigned:
		if (_newOverrideTarget == OverrideTarget)
		{
			return true;
		}

		// Adopt new override target:
		OverrideTarget = _newOverrideTarget;

		OverrideTargetChanged?.Invoke(this, prevOverrideTarget, OverrideTarget);
		return true;
	}

	private bool CheckOrRecreateOwnCameraTarget()
	{
		if (ownTargetChecksum == OutputSettings.Checksum && ownTarget is not null && !ownTarget.IsDisposed)
		{
			return true;
		}

		// Purge any deprecated previous targets:
		ownTarget?.Dispose();

		// Create new targets:
		if (!CameraTargets.CreateFromOutputSettings(graphicsService, logger, OutputSettings, out ownTarget))
		{
			return false;
		}

		ownTargetChecksum = OutputSettings.Checksum;
		return true;
	}

	private void CheckAndClearRenderTargets(CommandList _cmdList, uint _passIndex)
	{
		// Determine if and which textures/buffers to clear:
		CameraClearingFlags currentPassFlags = CameraClearingFlags.EachPass;
		if (_passIndex == 0)
		{
			currentPassFlags |= CameraClearingFlags.EachFrame;
		}

		bool shouldClearColor = OutputSettings.HasColorTarget && (ClearingSettings.ClearColorTargets & currentPassFlags) != 0;
		bool shouldClearDepth = OutputSettings.HasDepthBuffer && (ClearingSettings.ClearDepthBuffer & currentPassFlags) != 0;
		bool shouldClearStencil = OutputSettings.HasStencilBuffer && (ClearingSettings.ClearStencilBuffer & currentPassFlags) != 0;

		// Clear color targets:
		if (shouldClearColor)
		{
			int colorTargetCount = CurrentTarget!.ColorTargets!.Length;
			int maxClearColorIdx = ClearingSettings.ColorValues.Count - 1;
			for (int i = 0; i < colorTargetCount; ++i)
			{
				RgbaFloat clearColor = ClearingSettings.ColorValues[Math.Min(i, maxClearColorIdx)];
				_cmdList.ClearColorTarget((uint)i, clearColor);
			}
		}

		// Clear depth/stencil buffer:
		if (shouldClearDepth && shouldClearStencil)
		{
			_cmdList.ClearDepthStencil(ClearingSettings.DepthValue, ClearingSettings.StencilValue);
		}
		else if (shouldClearDepth)
		{
			_cmdList.ClearDepthStencil(ClearingSettings.DepthValue);
		}
	}

	public bool OnConnectedToWindow(WindowHandle? _newConnectedWindow)
	{
		if (_newConnectedWindow is not null && ConnectedWindow is not null && ConnectedWindow.IsOpen)
		{
			logger.LogError("Cannot connect camera to a new window while it already has a live connection!");
			return false;
		}
		if (_newConnectedWindow == ConnectedWindow)
		{
			return true;
		}

		// If null, disconnect and use own camera targets again:
		if (_newConnectedWindow is null)
		{
			ConnectedWindow = null;

			SetOverrideCameraTarget_internal(null);
			return true;
		}

		ConnectedWindow = _newConnectedWindow;

		return MakeCameraOutputToWindowSwapchain();
	}

	public void OnWindowClosing(WindowHandle? _windowHandle)
	{
		if (IsDisposed)
		{
			return;
		}
		if (ConnectedWindow is null || ConnectedWindow != _windowHandle)
		{
			return;
		}

		ConnectedWindow = null;
		CameraTargets? prevOverrideTarget = OverrideTarget;

		SetOverrideCameraTarget_internal(null);

		prevOverrideTarget?.Dispose();
	}

	public void OnWindowClosed(WindowHandle? _windowHandle) { }

	public void OnWindowResized(WindowHandle _windowHandle, Rectangle _newBounds)
	{
		if (_windowHandle == ConnectedWindow)
		{
			MakeCameraOutputToWindowSwapchain();
		}
	}

	private bool MakeCameraOutputToWindowSwapchain()
	{
		if (IsDisposed)
		{
			return false;
		}

		Framebuffer outputFramebuffer = ConnectedWindow!.Swapchain.Framebuffer;

		// (Re)create swapchain camera target:
		if (!CameraTargets.CreateFromFramebuffer(logger, outputFramebuffer, false, out CameraTargets? backBufferTarget))
		{
			logger.LogError("Failed to create camera target around window swapchain framebuffer!", LogEntrySeverity.Critical);
			return false;
		}

		// Adjust camera output to match swapchain:
		CameraOutputSettings outputSettings = CameraOutputSettings.CreateFromFramebuffer(in outputFramebuffer);
		if (!SetOutputSettings(outputSettings))
		{
			backBufferTarget?.Dispose();
			logger.LogError("Failed to adjust camera output to window swapchain!");
			return false;
		}

		// Make camera output to swapchain:
		if (!SetOverrideCameraTarget_internal(backBufferTarget))
		{
			backBufferTarget?.Dispose();
			logger.LogError($"Failed to make camera output to window '{ConnectedWindow}'!");
			return false;
		}

		return true;
	}

	public void OnSwapchainSwapped(WindowHandle _windowHandle) { }

	#endregion
}
