using FragEngine.EngineCore.Windows;

namespace FragEngine.Graphics;

/// <summary>
/// Delegate for listener methods that respond when the graphics settings are about to change.
/// </summary>
/// <param name="_currentSettings">The current graphics settings.</param>
/// <param name="_newSettings">The new settings that are about to come into effect.</param>
public delegate void FuncGraphicsSettingsChanging(GraphicsSettings? _currentSettings, GraphicsSettings _newSettings);

/// <summary>
/// Delegate for listener methods that respond when the graphics settings have just changed.
/// </summary>
/// <param name="_previousSettings">The previous graphics settings, that no longer apply.</param>
/// <param name="_currentSettings">The current graphics settings, that have just come into effect.</param>
public delegate void FuncGraphicsSettingsChanged(GraphicsSettings? _previousSettings, GraphicsSettings _currentSettings);

/// <summary>
/// Delegate for listener methods that respond when the buffers of the main swapchain are swapped.
/// This is called at most once each frame, immediately after all draw calls have been executed.
/// </summary>
/// <remarks>
/// You may use this event to synchronize swapchains across all windows, since they should all
/// use the same graphics device and rendering execution path.
/// </remarks>
/// <param name="_mainWindowHandle">A handle to the main window.</param>
public delegate void FuncMainSwapchainSwapped(WindowHandle _mainWindowHandle);
