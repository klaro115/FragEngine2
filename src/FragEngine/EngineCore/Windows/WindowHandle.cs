using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace FragEngine.EngineCore.Windows;

/// <summary>
/// Handle for an engine window.
/// </summary>
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
	/// Gets or sets whether this window can be resized.
	/// </summary>
	public bool IsResizable
	{
		get => IsOpen && Window.Resizable;
		set
		{
			if (IsOpen) Window.Resizable = value;
		}
	}

	/// <summary>
	/// Gets the underlying SDL2 window instance.
	/// </summary>
	internal Sdl2Window Window { get; }
	/// <summary>
	/// Gets the swapchain through which the window's content is presented.
	/// </summary>
	public Swapchain Swapchain { get; }

	/// <summary>
	/// A unique ID number for this window.
	/// </summary>
	public int WindowId { get; }

	#endregion
	#region Constructors

	internal WindowHandle(WindowService _windowService, ILogger _logger, Sdl2Window _window, Swapchain _swapchain, int _windowId)
	{
		ArgumentNullException.ThrowIfNull(_windowService);
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_window);
		ArgumentNullException.ThrowIfNull(_swapchain);

		windowService = _windowService;
		logger = _logger;
		Window = _window;
		Swapchain = _swapchain;
		WindowId = _windowId;

		// Register events:
		Window.Closing += OnClosing;
		Window.Closed += OnClosed;
		Window.Resized += OnResized;
		Window.Moved += OnMoved;

		logger.LogMessage($"Window {_windowId} created.");
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

	/// <summary>
	/// Changes the size of this window.
	/// </summary>
	/// <param name="_newWidth">The new horizontal size of the window.</param>
	/// <param name="_newHeight">The new vertical size of the window.</param>
	/// <returns>True if the window's size was changed, false otherwise.</returns>
	public bool ResizeWindow(int _newWidth, int _newHeight)
	{
		if (!IsOpen)
		{
			logger.LogError("Cannot resize closed window!");
			return false;
		}
		if (!IsResizable)
		{
			logger.LogError($"Window {WindowId} does not allow resizing!");
			return false;
		}

		Window.Width = Math.Max(_newWidth, 8);
		Window.Height = Math.Max(_newHeight, 8);
		return true;
	}

	/// <summary>
	/// Changes the size of this window so it fills the entire screen.
	/// </summary>
	/// <param name="_setBorderless">Whether to switch the window to borderless mode.</param>
	/// <returns>True if the window's size was changed, false otherwise.</returns>
	public bool FillScreen(bool _setBorderless = true)
	{
		if (!IsOpen)
		{
			logger.LogError("Cannot resize closed window!");
			return false;
		}

		// Measure window's current screen size:
		if (!windowService.GetScreenIndex(new(Window.X, Window.Y), out int screenIdx))
		{
			screenIdx = 0;
		}
		if (!windowService.GetScreenMetrics(screenIdx, out _, out Vector2 screenResolution, out _))
		{
			logger.LogError("Cannot resize window; failed to measure screen resolution!");
			return false;
		}

		// Move window to screen origin, remove border if requested:
		if (_setBorderless)
		{
			Window.BorderVisible = false;
		}
		Window.X = 0;
		Window.Y = 0;

		bool resized = ResizeWindow((int)screenResolution.X, (int)screenResolution.Y);
		return resized;
	}

	private void OnClosing()
	{
		if (!IsOpen) return;

		Closing?.Invoke(this);
	}

	private void OnClosed()
	{
		if (IsDisposed) return;

		Closed?.Invoke(this);

		// Unregister SDL2 window events:
		Window.Closing -= OnClosing;
		Window.Closed -= OnClosed;
		Window.Resized -= OnResized;
		Window.Moved -= OnMoved;

		// Unregister handle from service:
		if (!windowService.IsDisposed)
		{
			windowService.RemoveWindow(this);
		}

		logger.LogMessage($"Window {WindowId} closed.");

		Dispose();
	}

	private void OnResized()
	{
		if (!IsOpen) return;

		// Update resolution of the window's swapchain:
		uint newSwapchainWidth = Math.Max((uint)Window.Width, 8);
		uint newSwapchainHeight = Math.Max((uint)Window.Height, 8);
		Swapchain.Resize(newSwapchainWidth, newSwapchainHeight);

		// Notify the window's users of the change:
		Resized?.Invoke(this, Window.Bounds);
	}

	private void OnMoved(Point _newPosition)
	{
		if (!IsOpen) return;

		Moved?.Invoke(this, new(_newPosition.X, _newPosition.Y));
	}

	#endregion
}
