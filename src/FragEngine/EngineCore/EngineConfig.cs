using FragEngine.Graphics;

namespace FragEngine.EngineCore;

/// <summary>
/// Engine settings that are read once at launch time.
/// These are settings and configuration options that require a full app restart to take effect.
/// </summary>
[Serializable]
public sealed class EngineConfig
{
	#region Properties

	/// <summary>
	/// The graphics configuration.
	/// </summary>
	public GraphicsConfig Graphics { get; init; } = new();

	#endregion
}
