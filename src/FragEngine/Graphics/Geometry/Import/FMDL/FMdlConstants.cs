namespace FragEngine.Graphics.Geometry.Import.FMDL;

/// <summary>
/// Constants for importing and exporting the FMDL 3D file format.
/// </summary>
public static class FMdlConstants
{
	#region Constants

	// IDENTIFIERS:

	/// <summary>
	/// File extension for the FMDL format.
	/// </summary>
	public const string fmdlFormatKey = ".fmdl";

	/// <summary>
	/// "Magic number" leading the file. These 4 bytes serve as an umambiguous identifier of the file format.
	/// </summary>
	public const uint magicNumbers = ((uint)'F' << 0) | ((uint)'M' << 8) | ((uint)'D' << 16) | ((uint)'L' << 24);

	// VERSIONS:

	/// <summary>
	/// Major version of the engine's newest supported file format revision.
	/// </summary>
	public const byte currentVersionMajor = 0;
	/// <summary>
	/// Minor version of the engine's newest supported file format revision.
	/// </summary>
	public const byte currentVersionMinor = 1;

	/// <summary>
	/// Major version of the engine's oldest file format revision that is still supported.
	/// </summary>
	public const byte minimumVersionMajor = 0;
	/// <summary>
	/// Minor version of the engine's oldest file format revision that is still supported.
	/// </summary>
	public const byte minimumVersionMinor = 0;

	// HEADERS:
	
	/// <summary>
	/// Flags for mandatory headers that need to be raised. The format cannot be processed without these.
	/// </summary>
	public const FMdlHeaderFlags mandatoryHeaderFlags = FMdlHeaderFlags.File | FMdlHeaderFlags.Geometry;

	#endregion
}
