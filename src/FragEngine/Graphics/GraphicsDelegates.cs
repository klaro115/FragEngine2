using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics.Settings;

namespace FragEngine.Graphics;

/// <summary>
/// Delegate for listener methods that respond when the display settings are about to change.<para/>
/// This event is fired by the <see cref="WindowService"/>.
/// </summary>
/// <param name="_currentSettings">The current display settings.</param>
/// <param name="_newSettings">The new settings that are about to come into effect.</param>
public delegate void FuncDisplaySettingsChanging(in DisplaySettings? _currentSettings, DisplaySettings _newSettings);

/// <summary>
/// Delegate for listener methods that respond when the display settings have just changed.<para/>
/// This event is fired by the <see cref="WindowService"/>.
/// </summary>
/// <param name="_previousSettings">The previous display settings, that no longer apply.</param>
/// <param name="_currentSettings">The current display settings, that have just come into effect.</param>
public delegate void FuncDisplaySettingsChanged(in DisplaySettings? _previousSettings, DisplaySettings _currentSettings);

/// <summary>
/// Delegate for listener methods that respond when the graphics settings are about to change.<para/>
/// This event is fired by the <see cref="GraphicsService"/>.
/// </summary>
/// <param name="_currentSettings">The current graphics settings.</param>
/// <param name="_newSettings">The new settings that are about to come into effect.</param>
public delegate void FuncGraphicsSettingsChanging(GraphicsSettings? _currentSettings, GraphicsSettings _newSettings);

/// <summary>
/// Delegate for listener methods that respond when the graphics settings have just changed.<para/>
/// This event is fired by the <see cref="GraphicsService"/>.
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

/// <summary>
/// Delegate for listener methods that respond when the <see cref="GraphicsService"/> has started
/// a new frame.
/// </summary>
/// <remarks>
/// You may use this event to start behaviors that require the previous frame's draw calls to have
/// finished, and the current frame's logic to be up-to-date.
/// </remarks>
/// <param name="_frameIndex">The index of the new frame, as provided by the <see cref="TimeService"/>.</param>
public delegate void FuncFrameStarted(uint _frameIndex);
