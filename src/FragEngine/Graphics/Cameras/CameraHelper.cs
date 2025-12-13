using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using FragEngine.Scenes;
using Microsoft.Extensions.DependencyInjection;
using System.Numerics;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// Helper class for common camera and projection logic.
/// </summary>
public static class CameraHelper
{
	#region Methods

	/// <summary>
	/// Try to calculates a camera's projection matrix.
	/// </summary>
	/// <param name="_outputSettings">The camera's output settings. This tells us the resolution and aspect ratio.</param>
	/// <param name="_projectionSettings">The camera's projection settings. This tells us the projection behaviour.</param>
	/// <param name="_cameraWorldPose">The camera's pose (position, rotation, scale) in world space.</param>
	/// <param name="_outMtxWorld2Clip">Outputs a matrix that transforms a coordinate from world space to clip space.</param>
	/// <param name="_outMtxClip2World">Outputs a matrix that transforms a coordinate from clip space to world space.</param>
	/// <exception cref="ArgumentNullException">Output settings and projection settings may not be null.</exception>
	public static void CalculateProjectionMatrices(
		in CameraOutputSettings _outputSettings,
		in CameraProjectionSettings _projectionSettings,
		in Pose _cameraWorldPose,
		out Matrix4x4 _outMtxWorld2Clip,
		out Matrix4x4 _outMtxClip2World)
	{
		ArgumentNullException.ThrowIfNull(_outputSettings);
		ArgumentNullException.ThrowIfNull(_projectionSettings);

		// World space => Local space:
		if (!Matrix4x4.Invert(_cameraWorldPose.WorldMatrix, out Matrix4x4 mtxWorld2Local))
		{
			mtxWorld2Local = Matrix4x4.Identity;
		}

		// Local space => Clip space:
		Matrix4x4 mtxLocal2Clip;
		if (_projectionSettings.ProjectionType == CameraProjectionType.Perspective)
		{
			mtxLocal2Clip = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
				_projectionSettings.FieldOfViewRadians,
				_outputSettings.AspectRatio,
				_projectionSettings.NearClipPlane,
				_projectionSettings.FarClipPlane);
		}
		else
		{
			mtxLocal2Clip = Matrix4x4.CreateOrthographicLeftHanded(
				_projectionSettings.OrthographicSize * _outputSettings.AspectRatio,
				_projectionSettings.OrthographicSize,
				_projectionSettings.NearClipPlane,
				_projectionSettings.FarClipPlane);
		}

		// Combined: World space => Clip space:
		_outMtxWorld2Clip = mtxWorld2Local * mtxLocal2Clip;

		// Inverse: Clip space => World space:
		if (!Matrix4x4.Invert(_outMtxWorld2Clip, out _outMtxClip2World))
		{
			_outMtxClip2World = Matrix4x4.Identity;
		}
	}

	/// <summary>
	/// Creates a new camera with perspactive projection.
	/// </summary>
	/// <remarks>
	/// This is a helper method for creating a generic perspective camera as a one-liner. If your application requires a more
	/// custom camera setup, such as using non-standard depth/stencil behaviours and specfic pixel formats, it is recommended to
	/// create the camera manually and adjust its settings yourself.
	/// </remarks>
	/// <param name="_serviceProvider">The engine's service provider through which the camera and its services may be created.</param>
	/// <param name="_outCamera">Outputs the newly created camera, or null, if creation and setup fail.</param>
	/// <param name="_fieldOfViewDegrees">The vertical opening angle of the camera's viewport frustum, in degrees. Must be a number
	/// between 0 and 180.</param>
	/// <param name="_nearClipPlane">Gets the neareast clipping plane distance, in meters. Must be a value between 0.1mm and 10km.
	/// This is the distance at which objects inside of the camera's viewport are cut off.</param>
	/// <param name="_farClipPlane">Gets the far clipping plane distance, in meters. Must be greater than '_nearClipPlane', and
	/// less than 100km. This is the maximum distance beyond which objects inside of the camera's viewport are cut off.</param>
	/// <param name="_resolutionX">The horizontal output resolution, in pixels. Must be in the range from 8 to 8192, should be a
	/// multiple of 8.</param>
	/// <param name="_resolutionY">The vertical output resolution, in pixels. Must be in the range from 8 to 8192, should be a
	/// multiple of 8.</param>
	/// <param name="_poseSource">Optional. If non-null, this pose source will provide the camera with its position and orientation
	/// when rendering. To supply a pose value directly, you may pass that via a <see cref="ConstantPoseSource"/>. If null, the
	/// will be created at the coordinate origin instead.</param>
	/// <param name="_attachToWindowHandle">Optional. If non-null, the camera's output will be connected to this window's swapchain.
	/// The window's resolution will override any other resolution parameters passed to this method.</param>
	/// <returns>True if the camera was created and set up successfully, false otherwise.</returns>
	/// <exception cref="ArgumentException">If non-null, the output window handle may not be disposed or closed.</exception>
	/// <exception cref="ArgumentNullException">Service provider may not be null.</exception>
	/// <exception cref="InvalidOperationException">Service provider is missing required services, such as a logger.</exception>
	public static bool CreatePerspectiveCamera(
		IServiceProvider _serviceProvider,
		out Camera? _outCamera,
		float _fieldOfViewDegrees = CameraConstants.defaultFieldOfViewDegrees,
		float _nearClipPlane = CameraConstants.defaultNearClipPlane,
		float _farClipPlane = CameraConstants.defaultFarClipPlane,
		uint _resolutionX = CameraConstants.defaultOutputResolutionX,
		uint _resolutionY = CameraConstants.defaultOutputResolutionY,
		IPoseSource? _poseSource = null,
		WindowHandle? _attachToWindowHandle = null)
	{
		ArgumentNullException.ThrowIfNull(_serviceProvider);

		if (_attachToWindowHandle is not null && !_attachToWindowHandle.IsOpen)
		{
			throw new ArgumentException("Window for camera output may not be disposed or closed!", nameof(_attachToWindowHandle));
		}

		ILogger logger = _serviceProvider.GetRequiredService<ILogger>();

		// First, create and validate camera settings:
		CameraProjectionSettings projSettings = new()
		{
			ProjectionType = CameraProjectionType.Perspective,
			NearClipPlane = _nearClipPlane,
			FarClipPlane = _farClipPlane,
			FieldOfViewDegrees = _fieldOfViewDegrees,
		};
		CameraOutputSettings outputSettings = new()
		{
			ResolutionX = _attachToWindowHandle is not null
				? (uint)_attachToWindowHandle.Window.Width
				: _resolutionX,
			ResolutionY = _attachToWindowHandle is not null
				? (uint)_attachToWindowHandle.Window.Height
				: _resolutionY,
		};
		if (!projSettings.IsValid() || !outputSettings.IsValid())
		{
			logger.LogError("Cannot create perspective camera; invalid projection or output settings!");
			_outCamera = null;
			return false;
		}

		// Try creating the camera instance:
		try
		{
			_outCamera = _serviceProvider.GetRequiredService<Camera>();
		}
		catch (InvalidOperationException)
		{
			logger.LogError($"Cannot create perspective camera; Type {nameof(Camera)} has not been added to service provider!");
			_outCamera = null;
			return false;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create perspective camera!", ex, LogEntrySeverity.Normal);
			_outCamera = null;
			return false;
		}

		// Assign the pose source, if provided:
		if (_poseSource is not null)
		{
			_outCamera.CurrentPoseSource = _poseSource;
		}

		// Configure projection:
		if (!_outCamera.SetProjectionSettings(projSettings))
		{
			logger.LogError("Failed to apply projection settings to newly created camera instance!");
			_outCamera.Dispose();
			_outCamera= null;
			return false;
		}

		// Configure output:
		if (!_outCamera.SetOutputSettings(outputSettings))
		{
			logger.LogError("Failed to apply output settings to newly created camera instance!");
			_outCamera.Dispose();
			_outCamera = null;
			return false;
		}

		// Attach camera's output to a window's swapchain, if provided:
		if (_attachToWindowHandle is not null && !_attachToWindowHandle.ConnectClient(_outCamera))
		{
			logger.LogError("Failed to attach output of newly created camera instance to window!");
			_outCamera.Dispose();
			_outCamera = null;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Creates a new camera with ortographic projection.
	/// </summary>
	/// <remarks>
	/// This is a helper method for creating a generic ortographic camera as a one-liner. If your application requires a more
	/// custom camera setup, such as using non-standard depth/stencil behaviours and specfic pixel formats, it is recommended to
	/// create the camera manually and adjust its settings yourself.
	/// </remarks>
	/// <param name="_serviceProvider">The engine's service provider through which the camera and its services may be created.</param>
	/// <param name="_outCamera">Outputs the newly created camera, or null, if creation and setup fail.</param>
	/// <param name="_orthographicSize">The height of the camera's ortographic viewport, in meters. Must be a number between 0.1mm
	/// and 10km.</param>
	/// <param name="_nearClipPlane">Gets the neareast clipping plane distance, in meters. Must be a value between 0.1mm and 10km.
	/// This is the distance at which objects inside of the camera's viewport are cut off.</param>
	/// <param name="_farClipPlane">Gets the far clipping plane distance, in meters. Must be greater than '_nearClipPlane', and
	/// less than 100km. This is the maximum distance beyond which objects inside of the camera's viewport are cut off.</param>
	/// <param name="_resolutionX">The horizontal output resolution, in pixels. Must be in the range from 8 to 8192, should be a
	/// multiple of 8.</param>
	/// <param name="_resolutionY">The vertical output resolution, in pixels. Must be in the range from 8 to 8192, should be a
	/// multiple of 8.</param>
	/// <param name="_poseSource">Optional. If non-null, this pose source will provide the camera with its position and orientation
	/// when rendering. To supply a pose value directly, you may pass that via a <see cref="ConstantPoseSource"/>. If null, the
	/// will be created at the coordinate origin instead.</param>
	/// <param name="_attachToWindowHandle">Optional. If non-null, the camera's output will be connected to this window's swapchain.
	/// The window's resolution will override any other resolution parameters passed to this method.</param>
	/// <returns>True if the camera was created and set up successfully, false otherwise.</returns>
	/// <exception cref="ArgumentException">If non-null, the output window handle may not be disposed or closed.</exception>
	/// <exception cref="ArgumentNullException">Service provider may not be null.</exception>
	/// <exception cref="InvalidOperationException">Service provider is missing required services, such as a logger.</exception>
	public static bool CreateOrthographicsCamera(
		IServiceProvider _serviceProvider,
		out Camera? _outCamera,
		float _orthographicSize = CameraConstants.defaultOrthographicSize,
		float _nearClipPlane = CameraConstants.defaultNearClipPlane,
		float _farClipPlane = CameraConstants.defaultFarClipPlane,
		uint _resolutionX = CameraConstants.defaultOutputResolutionX,
		uint _resolutionY = CameraConstants.defaultOutputResolutionY,
		IPoseSource? _poseSource = null,
		WindowHandle? _attachToWindowHandle = null)
	{
		ArgumentNullException.ThrowIfNull(_serviceProvider);

		if (_attachToWindowHandle is not null && !_attachToWindowHandle.IsOpen)
		{
			throw new ArgumentException("Window for camera output may not be disposed or closed!", nameof(_attachToWindowHandle));
		}

		ILogger logger = _serviceProvider.GetRequiredService<ILogger>();

		// First, create and validate camera settings:
		CameraProjectionSettings projSettings = new()
		{
			ProjectionType = CameraProjectionType.Orthographic,
			NearClipPlane = _nearClipPlane,
			FarClipPlane = _farClipPlane,
			OrthographicSize = _orthographicSize,
		};
		CameraOutputSettings outputSettings = new()
		{
			ResolutionX = _attachToWindowHandle is not null
				? (uint)_attachToWindowHandle.Window.Width
				: _resolutionX,
			ResolutionY = _attachToWindowHandle is not null
				? (uint)_attachToWindowHandle.Window.Height
				: _resolutionY,
		};
		if (!projSettings.IsValid() || !outputSettings.IsValid())
		{
			logger.LogError("Cannot create ortographic camera; invalid projection or output settings!");
			_outCamera = null;
			return false;
		}

		// Try creating the camera instance:
		try
		{
			_outCamera = _serviceProvider.GetRequiredService<Camera>();
		}
		catch (InvalidOperationException)
		{
			logger.LogError($"Cannot create ortographic camera; Type {nameof(Camera)} has not been added to service provider!");
			_outCamera = null;
			return false;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create ortographic camera!", ex, LogEntrySeverity.Normal);
			_outCamera = null;
			return false;
		}

		// Assign the pose source, if provided:
		if (_poseSource is not null)
		{
			_outCamera.CurrentPoseSource = _poseSource;
		}

		// Configure projection:
		if (!_outCamera.SetProjectionSettings(projSettings))
		{
			logger.LogError("Failed to apply projection settings to newly created camera instance!");
			_outCamera.Dispose();
			_outCamera = null;
			return false;
		}

		// Configure output:
		if (!_outCamera.SetOutputSettings(outputSettings))
		{
			logger.LogError("Failed to apply output settings to newly created camera instance!");
			_outCamera.Dispose();
			_outCamera = null;
			return false;
		}

		// Attach camera's output to a window's swapchain, if provided:
		if (_attachToWindowHandle is not null && !_attachToWindowHandle.ConnectClient(_outCamera))
		{
			logger.LogError("Failed to attach output of newly created camera instance to window!");
			_outCamera.Dispose();
			_outCamera = null;
			return false;
		}

		return true;
	}

	#endregion
}
