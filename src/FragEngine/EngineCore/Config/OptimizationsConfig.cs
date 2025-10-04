namespace FragEngine.EngineCore.Config;

/// <summary>
/// Engine settings pertaining to optimizations and performance management.
/// </summary>
[Serializable]
public sealed class OptimizationsConfig
{
	#region Properties
	
	/// <summary>
	/// Whether to trigger garbage collection whenever the engine's state has changed.
	/// </summary>
	public bool GarbageCollectIfStateChanged { get; init; } = true;

	#endregion
}
