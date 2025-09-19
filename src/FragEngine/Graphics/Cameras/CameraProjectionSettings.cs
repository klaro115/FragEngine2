using FragEngine.Constants;
using FragEngine.Interfaces;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// A settings object that describes the desired projection behaviour of a <see cref="Camera"/>.
/// </summary>
/// <remarks>
/// Note: Resolution and aspect ratio of a camera image is determined by the camera's <see cref="CameraOutputSettings"/>.
/// </remarks>
public sealed class CameraProjectionSettings : IValidated, IChecksumVersioned
{
	#region Fields

	private ulong checksum = 0ul;

	private float nearClipPlane = 0.1f;
	private float farClipPlane = 1000.0f;
	private float fieldOfViewRad = 60.0f * MathConstants.Deg2Rad;
	private float orthographicsSize = 5.0f;

	#endregion
	#region Constants

	private const float CLIPPING_EPSILON = 0.0001f;

	#endregion
	#region Properties

	/// <summary>
	/// The type of projection to use. Basically, whether to use perspective or not.
	/// </summary>
	public required CameraProjectionType ProjectionType { get; init; }

	/// <summary>
	/// Gets the neareast clipping plane distance, in meters. Must be a value between 0.1mm and 10km.
	/// This is the distance at which objects inside of the camera's viewport are cut off.
	/// </summary>
	public required float NearClipPlane
	{
		get => nearClipPlane;
		init => nearClipPlane = Math.Clamp(value, CLIPPING_EPSILON, 1.0e+4f);
	}

	/// <summary>
	/// Gets the far clipping plane distance, in meters. Must be greater than <see cref="NearClipPlane"/>,
	/// and less than 100km. This is the maximum distance beyond which objects inside of the camera's viewport
	/// are cut off.
	/// </summary>
	public required float FarClipPlane
	{
		get => farClipPlane;
		init => farClipPlane = Math.Clamp(value, NearClipPlane + CLIPPING_EPSILON, 1.0e+5f);
	}

	/// <summary>
	/// Gets the field of view, in degrees. This is the opening angle of the viewport frustum along the vertical
	/// image axis. This value only applies if perspective projection is used.
	/// </summary>
	public float FieldOfViewDegrees
	{
		get => fieldOfViewRad * MathConstants.Rad2Deg;
		init => fieldOfViewRad = Math.Clamp(value, 0.001f, 179.9f) * MathConstants.Deg2Rad;
	}

	/// <summary>
	/// Gets the field of view, in radians. This is the opening angle of the viewport frustum along the vertical
	/// image axis. This value only applies if perspective projection is used.
	/// </summary>
	public float FieldOfViewRadians
	{
		get => fieldOfViewRad;
		init => fieldOfViewRad = Math.Clamp(value, 0.001f * MathConstants.Deg2Rad, 179.9f * MathConstants.Deg2Rad);
	}

	/// <summary>
	/// Gets the height of the orthographic projection frustum, in meters. Must be a value between 0.1mm and 10km.
	/// This value only applies if orthographic projection is used.
	/// </summary>
	public float OrthographicSize
	{
		get => orthographicsSize;
		init => orthographicsSize = Math.Clamp(value, CLIPPING_EPSILON, 1.0e+4f);
	}

	/// <summary>
	/// Gets or calculates a checksum for the current settings.
	/// This value may be used to unambiguously compare if 2 settings objects are identical, and is used for detecting changes.
	/// </summary>
	public ulong Checksum
	{
		get
		{
			if (checksum != 0)
			{
				return checksum;
			}

			checksum = CalculateChecksum();
			return checksum;
		}
	}

	/// <summary>
	/// Gets a set of default placeholder settings that should work in most situations.
	/// </summary>
	public static CameraProjectionSettings Default => new()
	{
		ProjectionType = CameraProjectionType.Perspective,
		NearClipPlane = 0.1f,
		FarClipPlane = 1000.0f,
		FieldOfViewDegrees = 60.0f,
	};

	#endregion
	#region Methods

	private ulong CalculateChecksum()
	{
		ulong newChecksum = 0ul;

		// Type:
		newChecksum |= (ulong)ProjectionType;
		
		// Clipping planes:
		{
			ulong ncpMM = (ulong)(NearClipPlane * 1000);    // 0..10M  = 24-bit
			ulong fcpMM = (ulong)(FarClipPlane * 1000);     // 0..100M = 27-bit
			ulong cpCombined = (ncpMM << 7) ^ fcpMM;
			newChecksum |= cpCombined << 1;
		}
		
		// Ortho/persp:
		if (ProjectionType == CameraProjectionType.Perspective)
		{
			ulong fov = (ulong)(FieldOfViewDegrees * 1000); // 1..180K = 18-bit
			newChecksum |= fov << 32;
		}
		else
		{
			ulong osMM = (ulong)(orthographicsSize * 1000); // 1..10M = 24-bit
			newChecksum |= osMM << 32;
		}

		return newChecksum;
	}

	/// <summary>
	/// Checks whether these projection settings are valid and make sense.
	/// </summary>
	/// <returns>True if valid, false otherwise.</returns>
	public bool IsValid()
	{
		bool isValid = FarClipPlane > NearClipPlane;
		return isValid;
	}

	public override string ToString()
	{
		return ProjectionType switch
		{
			CameraProjectionType.Perspective => $"Projection: {ProjectionType}, NearClipPlane: {NearClipPlane:0.###}m, FarClipPlane: {FarClipPlane:0.###}m, FOV: {FieldOfViewDegrees:0.##}°",
			CameraProjectionType.Orthographic => $"Projection: {ProjectionType}, NearClipPlane: {NearClipPlane:0.###}m, FarClipPlane: {FarClipPlane:0.###}m, OrthoSize: {OrthographicSize:0.###}m",
			_ => "Projection: <Invalid>",
		};
	}

	#endregion
}
