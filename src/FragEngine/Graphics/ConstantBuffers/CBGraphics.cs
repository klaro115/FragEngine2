using System.Runtime.InteropServices;
using Veldrid;

namespace FragEngine.Graphics.ConstantBuffers;

[StructLayout(LayoutKind.Sequential, Pack = sizeof(float), Size = byteSize)]
public struct CBGraphics
{
	#region Fields

	// Time data:
	public float appTime;
	public float levelTime;
	public float ingameTime;

	public float deltaTime;
	public float frameRate;
	public uint frameIndex;

	// Graphics data:
	public uint windowCount;
	public uint sceneCount;
	public uint cameraCount;

	//...

	#endregion
	#region Constants

	public const int byteSize =
		5 * sizeof(float) +
		4 * sizeof(uint);	// = 36 bytes

	public const int packedByteSize = 48;

	#endregion
	#region Properties

	/// <summary>
	/// Gets default initial data for this constant buffer type.
	/// </summary>
	public static CBGraphics Default => new()
	{
		// Time data:
		appTime = 0,
		levelTime = 0,
		ingameTime = 0,

		deltaTime = 0.01666667f,
		frameRate = 60,
		frameIndex = 0,

		// Graphics data:
		windowCount = 1,
		sceneCount = 1,
		cameraCount = 1,

		//...
	};

	/// <summary>
	/// Gets the GPU buffer description for this constant buffer type.
	/// </summary>
	public static BufferDescription BufferDesc => new(packedByteSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic);

	#endregion
}
