using FragEngine.Graphics.Contexts;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// Delegate for listener methods that respond when '<see cref="Camera.BeginFrame(in SceneContext, uint)"/>' is called.
/// </summary>
/// <param name="_camera">The camera that just started drawing a new frame.</param>
/// <param name="_cameraIdx">The index of this camera.<para/>
/// NOTE: This identifies a camera within a frame of a scene, and is not a stable identifier. This value may be used to
/// identify the camera in shader code, and to map frame-specific resources to a camera.</param>
/// <param name="_frameIdx">The index of the new frame.<para/>
/// NOTE: Note that this value is not specific to this camera, but instead identifies the scene-wide or even engine-wide
/// update cycle.</param>
public delegate void FuncCameraFrameStarted(Camera _camera, uint _cameraIdx, uint _frameIdx);

/// <summary>
/// Delegate for listener methods that respond when '<see cref="Camera.EndFrame()"/>' is called.
/// </summary>
/// <param name="_camera">The camera that just started finished drawing a frame.</param>
/// <param name="_cameraIdx">The index of this camera.<para/>
/// NOTE: This identifies a camera within a frame of a scene, and is not a stable identifier. This value may be used to
/// identify the camera in shader
/// code, and to map frame-specific resources to a camera.</param>
/// <param name="_frameIdx">The index of the frame that just ended.<para/>
/// NOTE: Note that this value is not specific to this camera, but instead identifies the scene-wide or even engine-wide
/// update cycle.</param>
public delegate void FuncCameraFrameEnded(Camera _camera, uint _cameraIdx, uint _frameIdx);

/// <summary>
/// Delegate for listener methods that respond when a camera's output settings have been updated.
/// </summary>
/// <param name="_camera">The camera whose '<see cref="Camera.OutputSettings"/>' have changed.</param>
/// <param name="_oldSettings">The previous output settings, that are now out-of-date.</param>
/// <param name="_newSettings">The new output settings, that are now coming into effect.</param>
public delegate void FuncCameraOutputSettingsChanged(Camera _camera, CameraOutputSettings _oldSettings, CameraOutputSettings _newSettings);

/// <summary>
/// Delegate for listener methods that respond when a camera's projection settings have been updated.
/// </summary>
/// <param name="_camera">The camera whose '<see cref="Camera.ProjectionSettings"/>' have changed.</param>
/// <param name="_oldSettings">The previous projection settings, that are now out-of-date.</param>
/// <param name="_newSettings">The new projection settings, that are now coming into effect.</param>
public delegate void FuncCameraProjectionSettingsChanged(Camera _camera, CameraProjectionSettings _oldSettings, CameraProjectionSettings _newSettings);

/// <summary>
/// Delegate for listener methods that respond when a camera's clearing settings have been updated.
/// </summary>
/// <param name="_camera">The camera whose '<see cref="Camera.ClearingSettings"/>' have changed.</param>
/// <param name="_oldSettings">The previous clearing settings, that are now out-of-date.</param>
/// <param name="_newSettings">The new clearing settings, that are now coming into effect.</param>
public delegate void FuncCameraClearingSettingsChanged(Camera _camera, CameraClearingSettings _oldSettings, CameraClearingSettings _newSettings);

/// <summary>
/// Delegate for listener methods that respond when an override render target has been assigned to, or unassigned from a camera.
/// </summary>
/// <param name="_camera">The camera whose '<see cref="Camera.OverrideTarget"/>' has changed.</param>
/// <param name="_oldOverrideTarget">The previous override camera target, that is no longer being used. If null, no override
/// was previously assigned.</param>
/// <param name="_newOverrideTarget">The newly assigned override camera target, that will be drawn to in an upcoming frame
/// or camera pass. If null, the override target was unassigned, and the camera is back to using its own internal targets.</param>
public delegate void FuncCameraOverrideTargetChanged(Camera _camera, CameraTargets? _oldOverrideTarget, CameraTargets? _newOverrideTarget);
