using FragEngine.Graphics;
using FragEngine.Interfaces;

namespace FragEngine.EngineCore.Config;

/// <summary>
/// Engine settings that are read once at launch time.
/// These are settings and configuration options that require a full app restart to take effect.
/// </summary>
[Serializable]
public sealed class EngineConfig : IValidated
{
	#region Properties

	public required EngineStartupConfig Startup { get; init; } = new() { CreateMainWindowImmediately = true };

	/// <summary>
	/// The graphics configuration.
	/// </summary>
	public required GraphicsConfig Graphics { get; init; } = new() { PreferNativeGraphicsAPI = true };

	/// <summary>
	/// Optimization and performance configuration.
	/// </summary>
	public required OptimizationsConfig Optimizations { get; init; } = new();

	#endregion
	#region Methods

	/// <summary>
	/// Creates a very basic one-size-fits-all config. This will be used by default if the config can't be read from file.
	/// </summary>
	/// <returns>A default config.</returns>
	public static EngineConfig CreateDefault()
	{
		EngineConfig config = new()
		{
			Startup = new()
			{
				CreateMainWindowImmediately = true,
			},
			Graphics = new()
			{
				PreferNativeGraphicsAPI = true,
			},
			Optimizations = new(),
		};
		return config;
	}

	public bool IsValid()
	{
		if (Startup is null || Optimizations is null)
		{
			return false;
		}
		if (Graphics is null || !Graphics.IsValid())
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// Gets feature flags for the initialization of <see cref="GraphicsService"/>.
	/// </summary>
	/// <returns></returns>
	public GraphicsServiceInitFlags GetGraphicsInitFlags()
	{
		GraphicsServiceInitFlags initFlags = GraphicsServiceInitFlags.CreateDevice;

		if (Startup.CreateMainWindowImmediately)
		{
			initFlags |= GraphicsServiceInitFlags.CreateMainWindowAndSwapchain;
		}

		return initFlags;
	}

	#endregion
}
