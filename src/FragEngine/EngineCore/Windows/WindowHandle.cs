using FragEngine.Graphics;
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
	private readonly GraphicsService graphicsService;

	private readonly HashSet<IWindowClient> clients = [];

	private readonly SemaphoreSlim clientSemaphore = new(1, 1);

	#endregion
	#region Constants

	private const int clientSemaphoreTimeoutMs = 30;

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

	internal WindowHandle(WindowService _windowService, ILogger _logger, GraphicsService _graphicsService, Sdl2Window _window, Swapchain _swapchain, int _windowId)
	{
		ArgumentNullException.ThrowIfNull(_windowService);
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ArgumentNullException.ThrowIfNull(_window);
		ArgumentNullException.ThrowIfNull(_swapchain);

		windowService = _windowService;
		logger = _logger;
		graphicsService = _graphicsService;
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

		clientSemaphore.Dispose();
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
	/// Gets the index of the screen this window is located on.
	/// </summary>
	/// <param name="_outScreenIdx">Outputs the index of the window's screen. On error, this is -1.</param>
	/// <returns>True if the screen index could be determined, false otherwise.</returns>
	public bool GetScreenIndex(out int _outScreenIdx)
	{
		if (!IsOpen)
		{
			logger.LogError("Cannot get screen index of closed window!");
			_outScreenIdx = -1;
			return false;
		}

		return windowService.GetScreenIndex(new(Window.X + 1, Window.Y + 1), out _outScreenIdx);
		// ^Note: Bias of +1 is added to prevent off-by-one errors along monitor edges.
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
		if (!GetScreenIndex(out int screenIdx))
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

	/// <summary>
	/// Moves the window to a different screen. The window's size will be clamped to the new screen's resolution.
	/// </summary>
	/// <param name="_screenIdx">The index of the screen the window should be moved to. May not be negative.</param>
	/// <returns>True if the window was moved to the designated screen, false otherwise.</returns>
	public bool MoveToScreen(int _screenIdx)
	{
		if (!IsOpen)
		{
			logger.LogError("Cannot move closed window to another screen!");
			return false;
		}

		// Check if we're already on the right screen:
		if (!GetScreenIndex(out int curScreenIdx))
		{
			curScreenIdx = 0;
		}
		if (curScreenIdx == _screenIdx)
		{
			return true;
		}

		// Get current and destination screen metrics:
		if (!windowService.GetScreenMetrics(curScreenIdx, out Vector2 curDesktopPosition, out _, out _))
		{
			logger.LogError("Cannot resize window; failed to measure current screen resolution!");
			return false;
		}
		if (!windowService.GetScreenMetrics(_screenIdx, out Vector2 dstDesktopPosition, out Vector2 dstScreenResolution, out _))
		{
			logger.LogError("Cannot resize window; failed to measure destination screen resolution!");
			return false;
		}

		// Calculate new coordinates, resize if too large for destination:
		Rectangle curBounds = Window.Bounds;
		Rectangle dstBounds = curBounds with
		{
			X = curBounds.X - (int)curDesktopPosition.X + (int)dstDesktopPosition.X,
			Y = curBounds.Y - (int)curDesktopPosition.Y + (int)dstDesktopPosition.Y,
		};

		if (curBounds.Width > dstScreenResolution.X)
		{
			dstBounds.X = 0;
			dstBounds.Width = Math.Max((int)dstScreenResolution.X, 8);
		}
		else if (curBounds.Right - curDesktopPosition.X > dstScreenResolution.X)
		{
			dstBounds.X = (int)dstDesktopPosition.X + (int)dstScreenResolution.X - dstBounds.Width;
		}

		if (curBounds.Height > dstScreenResolution.Y)
		{
			dstBounds.Y = 0;
			dstBounds.Height = Math.Max((int)dstScreenResolution.Y, 8);
		}
		else if (dstBounds.Bottom - dstScreenResolution.Y > 0)
		{
			dstBounds.Y = (int)dstDesktopPosition.Y + (int)dstScreenResolution.Y - dstBounds.Height;
		}

		// Apply changes:
		Window.X = dstBounds.X;
		Window.Y = dstBounds.Y;
		Window.Width = dstBounds.Width;
		Window.Height = dstBounds.Height;
		return true;
	}

	/// <summary>
	/// Connects a new client to this window. The client will receive lifecycle and resizing events for this window.
	/// </summary>
	/// <param name="_newClient">A new client to connect to this window</param>
	/// <returns>True if the client was connected to this window, false otherwise.</returns>
	public bool ConnectClient(IWindowClient _newClient)
	{
		if (_newClient is null)
		{
			logger.LogError("Cannot connect null client to window handle!");
			return false;
		}
		if (_newClient is IExtendedDisposable disposable && disposable.IsDisposed)
		{
			logger.LogError("Cannot connect disposed client to window handle!");
			return false;
		}
		if (!IsOpen)
		{
			logger.LogError("Cannot connect new client to window handle that has already been closed or disposed!");
			return false;
		}

		// Ensure exclusive connection to one window at a time:
		if (_newClient.ConnectedWindow is not null && _newClient.ConnectedWindow != this && _newClient.ConnectedWindow.IsOpen)
		{
			logger.LogError($"Window client '{_newClient}' is already connected to a window! Disconnect it first before attempting a new connection!");
			return false;
		}

		if (!clientSemaphore.Wait(clientSemaphoreTimeoutMs))
		{
			logger.LogError($"Timeout when trying to connect window client to window '{Window.Title}'!");
			return false;
		}

		try
		{
			// Add new client to the list:
			if (clients.Contains(_newClient))
			{
				logger.LogWarning("Window client has already been connected to this window.");
				return true;
			}

			// Create connection:
			if (!_newClient.OnConnectedToWindow(this))
			{
				logger.LogError($"Failed to establish connection client-side between client '{_newClient}' and window '{Window.Title}'!");
				return false;
			}

			clients.Add(_newClient);

			// Subscribe client to window events:
			Closing += _newClient.OnWindowClosing;
			Closed += _newClient.OnWindowClosed;
			Resized += _newClient.OnWindowResized;
			graphicsService.MainSwapchainSwapped += _newClient.OnSwapchainSwapped;

			return true;
		}
		finally
		{
			clientSemaphore.Release();
		}
	}

	/// <summary>
	/// Disconnects an existing client from this window.
	/// </summary>
	/// <param name="_client">A client that has previously been connected to this window.</param>
	/// <returns>True if the </returns>
	public bool DisconnectClient(IWindowClient _client)
	{
		if (_client is null)
		{
			logger.LogError("Cannot disconnect null client from window handle!");
			return false;
		}

		if (!clientSemaphore.Wait(clientSemaphoreTimeoutMs))
		{
			logger.LogError($"Timeout when trying to connect window client to window '{Window.Title}'!");
			return false;
		}

		try
		{
			if (!clients.Contains(_client))
			{
				logger.LogError($"Window client '{_client}' is not connected to window '{Window.Title}'!");
				return false;
			}

			// Cut connection and remove client from list:
			bool isClientDisposed = _client is IExtendedDisposable disposable && disposable.IsDisposed;
			if (!isClientDisposed && !_client.OnConnectedToWindow(null))
			{
				logger.LogError($"Failed to cut connection client-side between client '{_client}' and window '{Window.Title}'!");
				return false;
			}

			clients.Remove(_client);

			// Unsubscribe client from window events:
			Closing -= _client.OnWindowClosing;
			Closed -= _client.OnWindowClosed;
			Resized -= _client.OnWindowResized;
			graphicsService.MainSwapchainSwapped -= _client.OnSwapchainSwapped;

			return true;
		}
		finally
		{
			clientSemaphore.Release();
		}
	}

	private void OnClosing()
	{
		if (!IsOpen) return;

		Closing?.Invoke(this);
	}

	private void OnClosed()
	{
		if (IsDisposed) return;

		try
		{
			clientSemaphore.Wait(clientSemaphoreTimeoutMs);
			clients.Clear();
		}
		finally
		{
			clientSemaphore.Release();
		}

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
