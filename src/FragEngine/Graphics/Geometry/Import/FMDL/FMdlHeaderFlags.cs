namespace FragEngine.Graphics.Geometry.Import.FMDL;

/// <summary>
/// Flags of different headers that can be found in an 3D model file using the FMDL format.
/// </summary>
[Flags]
public enum FMdlHeaderFlags : ushort
{
	/// <summary>
	/// Main file header. This header is mandatory.
	/// </summary>
	File		= 1,
	/// <summary>
	/// Polygonal geometry header. This header is mandatory.
	/// </summary>
	Geometry	= 2,
	//...
}
