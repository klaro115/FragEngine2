﻿using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace FragEngine.Graphics.ConstantBuffers;

[StructLayout(LayoutKind.Sequential, Pack = sizeof(float), Size = byteSize)]
public struct CBScene
{
	#region Fields

	// Ambient lighting:
	public Vector4 ambientLightTopDown;
	public Vector4 ambientLightHorizon;
	public Vector4 ambientLightBottomUp;

	// Scene contents:
	public uint rendererCount;
	public uint lightCount;
	public uint lightCountShadowMapped;
	public uint cameraCount;

	//...

	#endregion
	#region Constants

	public const int byteSize = 3 * 4 * sizeof(float) + 4 * sizeof(uint);

	#endregion
	#region Properties

	/// <summary>
	/// Gets default initial data for this constant buffer type.
	/// </summary>
	public static CBScene Default => new()
	{
		// Ambient lighting:
		ambientLightTopDown = new(),
		ambientLightHorizon = new(),
		ambientLightBottomUp = new(),

		// Scene contents:
		rendererCount = 0,
		lightCount = 0,
		lightCountShadowMapped = 0,
		cameraCount = 0,

		//...
	};

	/// <summary>
	/// Gets the GPU buffer description for this constant buffer type.
	/// </summary>
	public static BufferDescription BufferDesc => new(byteSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic);

	#endregion
}
