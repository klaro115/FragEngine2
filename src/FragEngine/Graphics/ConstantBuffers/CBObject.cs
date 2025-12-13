using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace FragEngine.Graphics.ConstantBuffers;

[StructLayout(LayoutKind.Sequential, Pack = sizeof(float), Size = byteSize)]
public struct CBObject
{
	#region Fields

	public Matrix4x4 mtxWorld;
	public Vector3 minBounds;
	public Vector3 maxBounds;

	#endregion
	#region Constants

	public const string resourceName = nameof(CBObject);

	public const int byteSize =
		16 * sizeof(float) +
		2 * 3 * sizeof(float);	// = 88 bytes

	public const int packedByteSize = 96;

	#endregion
	#region Properties

	/// <summary>
	/// Gets default initial data for this constant buffer type.
	/// </summary>
	public static CBObject Default => new()
	{
		mtxWorld = Matrix4x4.Identity,
		minBounds = -Vector3.One,
		maxBounds = Vector3.One,
		//...
	};

	/// <summary>
	/// Gets the GPU buffer description for this constant buffer type.
	/// </summary>
	public static BufferDescription BufferDesc => new(packedByteSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic);

	/// <summary>
	/// Gets the resource layout for this constant buffer type.
	/// </summary>
	public static ResourceLayoutElementDescription ResourceLayoutElementDesc => new(resourceName, ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment);

	#endregion
}
