using Veldrid;

namespace FragEngine.EngineCore.Windows;

/// <summary>
/// Interface for client objects or services that are connected to a window.
/// This interface is typically attached to cameras or other image providers
/// that are directly tied to a specific window, and that need to respond if
/// the window's resolution or state changes.<para/>
/// USAGE: Call '<see cref="WindowHandle.ConnectClient(IWindowClient)"/>' to
/// connect a client to a window. All window events and references will be set
/// automatically.
/// </summary>
public interface IWindowClient
{
	#region Properties

	/// <summary>
	/// Gets the window that this client is currently connected to, or null,
	/// if it is not currently connected to any windows.
	/// </summary>
	WindowHandle? ConnectedWindow { get; }

	#endregion
	#region Methods

	/// <summary>
	/// Listener method for when this client is connected to a window.
	/// </summary>
	/// <remarks>
	/// This method should only ever be called internally by the window's
	/// connect/disconnect methods.
	/// </remarks>
	/// <param name="_newConnectedWindow"></param>
	/// <returns></returns>
	bool OnConnectedToWindow(WindowHandle? _newConnectedWindow);

	/// <summary>
	/// Listener method for when the window that this client is connected
	/// to is about to close.
	/// </summary>
	/// <param name="_windowHandle">A handle to the closing window.</param>
	void OnWindowClosing(WindowHandle? _windowHandle);

	/// <summary>
	/// Listener method for when the window that this client is connected
	/// to has closed.
	/// </summary>
	/// <param name="_windowHandle">A handle to the closed window.</param>
	void OnWindowClosed(WindowHandle? _windowHandle);

	/// <summary>
	/// Listener method for when the window that this client is connected
	/// to has been resized. This implies that the window's underlying
	/// swapchain has also been resized, and that the client's graphics
	/// resources might need to be adjusted to match the new resolution.
	/// </summary>
	/// <param name="_windowHandle">A handle to the resized window.</param>
	/// <param name="_newBounds">The new bounds (position & resolution)
	/// of the window after resizing.</param>
	void OnWindowResized(WindowHandle _windowHandle, Rectangle _newBounds);

	/// <summary>
	/// Listener method for when the backbuffers of the window that this
	/// client is connected to have been swapped. This signifies that a
	/// frame has finished rendering and is now being displayed.
	/// </summary>
	/// <param name="_windowHandle">A handle to the window.</param>
	void OnSwapchainSwapped(WindowHandle _windowHandle);

	#endregion
}
