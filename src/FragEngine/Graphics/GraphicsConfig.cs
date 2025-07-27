namespace FragEngine.Graphics;

/// <summary>
/// Graphics settings that are read once at launch time.
/// </summary>
[Serializable]
public sealed class GraphicsConfig
{
	#region Properties

	/// <summary>
	/// Whether to prefer the OS' native graphics API over a cross-platform API.
	/// </summary>
	/// <remarks>
	/// On Windows, the native API is Direct3D, and the cross-platform alternative is Vulkan.
	/// </remarks>
	public bool PreferNativeGraphicsAPI { get; init; } = true;

	#endregion
}
