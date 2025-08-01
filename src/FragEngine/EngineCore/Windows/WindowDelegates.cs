using System.Numerics;
using Veldrid;

namespace FragEngine.EngineCore.Windows;

/// <summary>
/// Event listener method for when a new window is created.
/// </summary>
/// <param name="_windowHandle">A handle to the new window.</param>
public delegate void FuncNewWindowCreated(WindowHandle _windowHandle);

/// <summary>
/// Event listener method for when a window was closed.
/// </summary>
/// <param name="_windowHandle">A handle to the expired window.</param>
public delegate void FuncWindowClosed(WindowHandle _windowHandle);

/// <summary>
/// Event listener method for when a window's size has changed.
/// </summary>
/// <param name="_windowHandle">A handle to the expired window.</param>
/// <param name="_newBounds">The new bounding box of the window.</param>
public delegate void FuncWindowResized(WindowHandle _windowHandle, Rectangle _newBounds);

/// <summary>
/// Event listener method for when a window's position has changed.
/// </summary>
/// <param name="_windowHandle">A handle to the expired window.</param>
/// <param name="_newPosition">The new position of the window.</param>
public delegate void FuncWindowMoved(WindowHandle _windowHandle, Vector2 _newPosition);

/// <summary>
/// Event listener method for when a different application window has been focused.
/// </summary>
/// <param name="_windowHandle">A handle to the window that is now focused.
/// Null if none of the application's windows are focused.</param>
public delegate void FuncWindowFocusChanged(WindowHandle? _windowHandle);
