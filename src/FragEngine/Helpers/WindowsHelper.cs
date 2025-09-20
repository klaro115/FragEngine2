using FragEngine.EngineCore.Windows;
using FragEngine.Extensions.SDL;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Veldrid.Sdl2;

namespace FragEngine.Helpers;

/// <summary>
/// Helper methods specifically for dealing with windows platforms.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowsHelper
{
	#region Properties

	/// <summary>
	/// Feazure switch; gets whether the feature 'dynamic code' is supported.
	/// </summary>
	[FeatureGuard(typeof(RequiresAssemblyFilesAttribute))]
	[FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
	[SuppressMessage("AOT", "IL4000:Return value does not match FeatureGuard annotations of the property. The check should return false whenever any of the features referenced in the FeatureGuard annotations is disabled.", Justification = "Dynamic code should cover both.")]
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Intellisense can't make up its mind whether to annoy me with squiggly lines or supposedly redundant suppression.")]
	private static bool IsDynamicCodeSupported => RuntimeFeature.IsDynamicCodeSupported;

	#endregion
	#region Methods

	/// <summary>
	/// Tries to get the HInstance of the app.
	/// </summary>
	/// <param name="_logger">A logging service for outputting errors.</param>
	/// <param name="_sdlWindow">An SDL window through which we can try to get the app's HInstance.</param>
	/// <param name="_outHInstance">Outputs the instance handle, or -1, on failure.</param>
	/// <returns>True if the HInstance could be determined, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Logger may not be null.</exception>
	public static bool TryGetAppHInstance(ILogger _logger, Sdl2Window? _sdlWindow, out nint _outHInstance)
	{
		ArgumentNullException.ThrowIfNull(_logger);

		// If dynamic code and assemblies were not trimmed, try those:
		if (IsDynamicCodeSupported)
		{
			_outHInstance = GetHInstanceFromModule();
			if (_outHInstance > 0)
			{
				return true;
			}
		}
		else
		{
			_logger.LogError("Cannot determine the app's HInstance!");
			_outHInstance = -1;
			return false;
		}

		// If an SDL window was provided, try to get the instance handle from there:
		if (_sdlWindow is not null &&
			_sdlWindow.Exists &&
			_sdlWindow.TryGetAppHInstance(_logger, out _outHInstance))
		{
			return true;
		}

		return _outHInstance > 0;
	}

	/// <summary>
	/// Tries to get the HInstance of the app.
	/// </summary>
	/// <param name="_logger">A logging service for outputting errors.</param>
	/// <param name="_windowHandle">A window handle through which we can try to get the app's HInstance.</param>
	/// <param name="_outHInstance">Outputs the instance handle, or -1, on failure.</param>
	/// <returns>True if the HInstance could be determined, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Logger may not be null.</exception>
	public static bool TryGetAppHInstance(ILogger _logger, WindowHandle _windowHandle, out nint _outHInstance)
	{
		Sdl2Window? sdlWindow = _windowHandle is not null && _windowHandle.IsOpen
			? _windowHandle.Window
			: null;

		return TryGetAppHInstance(_logger, sdlWindow, out _outHInstance);
	}

	[RequiresAssemblyFiles]
	private static nint GetHInstanceFromModule()
	{
		return Marshal.GetHINSTANCE(typeof(WindowsHelper).Module);
	}

	#endregion
}
