using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace FragEngine.Graphics.ConstantBuffers;

[StructLayout(LayoutKind.Sequential, Pack = sizeof(float), Size = byteSize)]
public struct CBCamera
{
	#region Fields

	// Clearing:
	public Vector4 backgroundColor;

	// Output format:
	public uint cameraIndex;
	public uint cameraPassIndex;
	public uint resolutionX;
	public uint resolutionY;
	public float nearClipPlane;
	public float farClipPlane;

	//...

	#endregion
	#region Constants

	public const int byteSize = 1 * 4 * sizeof(float) + 4 * sizeof(uint) + 2 * sizeof(float);

	#endregion
	#region Properties

	/// <summary>
	/// Gets default initial data for this constant buffer type.
	/// </summary>
	public static CBCamera Default => new()
	{
		// Clearing:
		backgroundColor = RgbaFloat.CornflowerBlue.ToVector4(),

		// Output format:
		cameraIndex = 0,
		cameraPassIndex = 0,
		resolutionX = 640,
		resolutionY = 480,
		nearClipPlane = 0.1f,
		farClipPlane = 1000.0f,

		//...
	};

	/// <summary>
	/// Gets the GPU buffer description for this constant buffer type.
	/// </summary>
	public static BufferDescription BufferDesc => new(byteSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic);

	#endregion
}
