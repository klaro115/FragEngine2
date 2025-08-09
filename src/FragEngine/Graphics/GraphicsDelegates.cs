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
