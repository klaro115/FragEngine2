using FragEngine.Scenes;
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
		Matrix4x4 mtxLocal2Clip = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
			_projectionSettings.FieldOfViewRadians,
			_outputSettings.AspectRatio,
			_projectionSettings.NearClipPlane,
			_projectionSettings.FarClipPlane);

		// Combined: World space => Clip space:
		_outMtxWorld2Clip = Matrix4x4.Multiply(mtxWorld2Local, mtxLocal2Clip);		//TODO [CRITICAL]: Check/Test if order is correct!

		// Inverse: Clip space => World space:
		if (!Matrix4x4.Invert(_outMtxWorld2Clip, out _outMtxClip2World))
		{
			_outMtxClip2World = Matrix4x4.Identity;
		}
	}

	#endregion
}
