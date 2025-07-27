using FragEngine.EngineCore.Windows;
using FragEngine.Interfaces;
using FragEngine.Logging;

namespace FragEngine.Graphics;

public abstract class GraphicsService(ILogger _logger, WindowService _windowService) : IExtendedDisposable
{
	#region Fields

	protected readonly ILogger logger = _logger;
	protected readonly WindowService windowService = _windowService;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	protected virtual void Dispose(bool _disposing)
	{
		IsDisposed = true;
	}

	internal abstract bool Initialize();
	internal abstract bool Shutdown();

	internal abstract bool Draw();

	#endregion
}
