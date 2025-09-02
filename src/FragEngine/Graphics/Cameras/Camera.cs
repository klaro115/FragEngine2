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
	public event Action? FrameStarted = null;
	/// <summary>
	/// Event that is triggered when the camera finishes drawing a frame.
	/// </summary>
	public event Action? FrameEnded = null;

	#endregion
	#region Fields

	private readonly GraphicsService graphicsService;
	private readonly ILogger logger;

	private bool isDrawingFrame = false;
	private bool isDrawingPass = false;

	private IPoseSource currentPoseSource;

	private CBCamera cbCameraData = CBCamera.Default;
	private readonly DeviceBuffer bufCbCamera;

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
		//TODO
	}

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

		// Update constant buffer data:
		{
			cbCameraData.backgroundColor = Vector4.Zero;	//TODO
			cbCameraData.cameraIndex = _cameraIndex;
			cbCameraData.cameraPassIndex = 0u;
			cbCameraData.resolutionX = 0;					//TODO
			cbCameraData.resolutionY = 0;					//TODO
			cbCameraData.nearClipPlane = 0;					//TODO
			cbCameraData.farClipPlane = 0;					//TODO
		}
		

		//TODO


		IsDrawingFrame = true;

		FrameStarted?.Invoke();
		return true;
	}

	public void EndFrame()
	{
		IsDrawingFrame = false;

		FrameEnded?.Invoke();
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

	#endregion
}
