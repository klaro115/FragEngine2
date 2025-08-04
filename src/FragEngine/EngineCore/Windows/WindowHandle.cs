using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace FragEngine.EngineCore.Windows;

public sealed class WindowHandle : IExtendedDisposable
{
	#region Events

	public event FuncWindowClosed? Closing;
	public event FuncWindowClosed? Closed;
	public event FuncWindowResized? Resized;
	public event FuncWindowMoved? Moved;

	#endregion
	#region Fields

	private readonly WindowService windowService;
	private readonly ILogger logger;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;
	/// <summary>
	/// Gets whether the window is currently open.
	/// </summary>
	public bool IsOpen => !IsDisposed && Window.Exists;
	/// <summary>
	/// Gets whether the window is currently visible on screen.
	/// </summary>
	public bool IVisible => IsOpen && IsOpen;

	/// <summary>
	/// Gets the underlying SDL2 window instance.
	/// </summary>
	internal Sdl2Window Window { get; }

	/// <summary>
	/// A unique ID number for this window.
	/// </summary>
	public int WindowId { get; }

	#endregion
	#region Constructors

	internal WindowHandle(WindowService _windowService, ILogger _logger, Sdl2Window _window, int _windowId)
	{
		ArgumentNullException.ThrowIfNull(_windowService);
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_window);

		windowService = _windowService;
		logger = _logger;
		Window = _window;
		WindowId = _windowId;

		// Register events:
		Window.Closing += () => Closing?.Invoke(this);
		Window.Closed += OnClosed;
		Window.Resized += OnResized;
		Window.Moved += OnMoved;
	}

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

	private void OnClosed()
	{
		if (IsDisposed) return;

		Closed?.Invoke(this);

		if (!windowService.IsDisposed)
		{
			windowService.RemoveWindow(this);
		}

		Dispose();
	}

	private void OnResized()
	{
		if (!IsOpen) return;

		Resized?.Invoke(this, Window.Bounds);
	}

	private void OnMoved(Point _newPosition)
	{
		if (!IsOpen) return;

		Moved?.Invoke(this, new(_newPosition.X, _newPosition.Y));
	}

	#endregion
}
