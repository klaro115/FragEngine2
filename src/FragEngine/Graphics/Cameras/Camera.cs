using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Contexts;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Scenes;
using System.Numerics;
using Veldrid;

namespace FragEngine.Graphics.Cameras;

public sealed class Camera : IExtendedDisposable
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
	private ulong projectionChecksum = 0ul;

	private CBCamera cbCameraData = CBCamera.Default;
	private readonly DeviceBuffer bufCbCamera;

	private CameraTargets? ownTarget = null;
	private ulong ownTargetChecksum = 0ul;

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
	/// Projection settings may be changed in-between frames using '<see cref="SetProjectionSettings(CameraProjectionSettings)"/>'.
	/// </summary>
	public CameraProjectionSettings ProjectionSettings { get; private set; } = CameraProjectionSettings.Default;
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

	private void Dispose(bool _)
	{
		IsDisposed = true;

		bufCbCamera?.Dispose();
		ownTarget?.Dispose();
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
	/// <returns></returns>
	public bool BeginFrame(in SceneContext _sceneCtx, uint _cameraIndex)
	{
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
			cbCameraData.mtxWorld2Clip = mtxWorld2Clip;
			cbCameraData.mtxClip2World = mtxClip2World;
			cbCameraData.backgroundColor = Vector4.Zero;	//TODO
			cbCameraData.cameraIndex = _cameraIndex;
			cbCameraData.cameraPassIndex = 0u;
			cbCameraData.resolutionX = 0;					//TODO
			cbCameraData.resolutionY = 0;					//TODO
			cbCameraData.nearClipPlane = 0;					//TODO
			cbCameraData.farClipPlane = 0;					//TODO
		}

		// Check if any previously assigned override targets are still alive:
		if (OverrideTarget is not null && OverrideTarget.IsDisposed)
		{
			logger.LogWarning("Override camera target was disposed; unassigning and falling back to internal targets.");

			if (!SetOverrideCameraTarget(null))
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


		//TODO


		IsDrawingFrame = true;
		currentFrameIndex = _sceneCtx.GraphicsCtx.CbGraphics.frameIndex;

		FrameStarted?.Invoke(this, _cameraIndex, currentFrameIndex);
		return true;
	}

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

	public bool BeginPass(in SceneContext _sceneCtx, CommandList _cmdList, uint _passIndex, out CameraPassContext? _outCameraPassCtx)
	{
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


		//TODO


		// Update constant buffer data:
		{
			cbCameraData.cameraPassIndex = _passIndex;
		}

		// Upload CBCamera to GPU buffer:
		_cmdList.UpdateBuffer(bufCbCamera, 0u, ref cbCameraData);

		// Assemble camera pass context:
		_outCameraPassCtx = new(_sceneCtx)
		{
			CmdList = _cmdList,
			CbCamera = cbCameraData,
			BufCbCamera = bufCbCamera!,
			//...
		};

		IsDrawingPass = true;
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
		ownTargetChecksum = 0;

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
		if (!CameraTargets.Create(graphicsService, logger, OutputSettings, out ownTarget))
		{
			return false;
		}

		ownTargetChecksum = OutputSettings.Checksum;
		return true;
	}

	#endregion
}
