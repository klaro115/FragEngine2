using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Numerics;
using Veldrid.Sdl2;

namespace FragEngine.EngineCore.Windows;

public sealed class WindowHandle(WindowService _windowService, ILogger _logger, Sdl2Window _window) : IExtendedDisposable
{
	#region Fields

	private readonly WindowService windowService = _windowService;
	private readonly ILogger logger = _logger;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;
	public bool IsOpen => !IsDisposed && Window.Exists;
	public bool IVisible => IsOpen && IsOpen;

	internal Sdl2Window Window { get; } = _window ?? throw new ArgumentNullException(nameof(Window));

	#endregion
	#region Constructors

	~WindowHandle()
	{
		Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool _)
	{
		CloseWindow();

		IsDisposed = true;
	}

	/// <summary>
	/// Closes this window.
	/// </summary>
	public void CloseWindow()
	{
		if (Window.Exists)
		{
			Window.Close();
		}
	}

	/// <summary>
	/// Gets metrics about this window.
	/// </summary>
	/// <param name="_outDesktopPosition">Outputs the pixel position of the window in desktop space.</param>
	/// <param name="_outResolution">Outputs the pixel size of the window.</param>
	/// <returns>True if metrics were determined, false otherwise.</returns>
	public bool GetWindowMetrics(out Vector2 _outDesktopPosition, out Vector2 _outResolution)
	{
		if (!IsOpen)
		{
			logger.LogError("Cannot get metrics of closed window!");
			_outDesktopPosition = Vector2.Zero;
			_outResolution = Vector2.Zero;
			return false;
		}

		_outDesktopPosition = Window.Bounds.Position;
		_outResolution = Window.Bounds.Size;
		return true;
	}

	#endregion
}
