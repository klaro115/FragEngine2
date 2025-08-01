using FragEngine.EngineCore;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace FragEngine.Graphics.Dx11;

/// <summary>
/// Graphics service implementation for the Direct3D 11 graphics API.
/// </summary>
/// <param name="_logger">The logger service.</param>
internal sealed class Dx11GraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	EngineConfig _config)
	: GraphicsService(_logger, _platformService, _windowService, _config)
{
	#region Methods

	internal override bool Initialize(bool _createMainWindow)
	{
		if (IsDisposed)
		{
			logger.LogError($"Cannot initialize {nameof(Dx11GraphicsService)} that has already been disposed.", LogEntrySeverity.High);
			return false;
		}
		if (IsInitialized)
		{
			logger.LogWarning($"{nameof(Dx11GraphicsService)} is already initialized.");
			return true;
		}

		if (!CreateGraphicsDevice(_createMainWindow))
		{
			return false;
		}

		IsInitialized = true;
		return true;
	}

	internal override bool Shutdown()
	{
		if (!IsInitialized)
		{
			return false;
		}

		Device.Dispose();

		IsInitialized = false;
		return true;
	}

	internal override bool Draw()
	{
		throw new NotImplementedException();
	}

	private bool CreateGraphicsDevice(bool _createMainWindow)
	{
		GraphicsDeviceOptions deviceOptions = new(
			config.CreateDebug,
			PixelFormat.D24_UNorm_S8_UInt,
			config.VSync,
			ResourceBindingModel.Improved,
			true,
			true,
			config.OutputIsSRgb);

		// Create with window:
		if (_createMainWindow)
		{
			if (!GetWindowSettings(out string windowTitle, out Vector2 windowPosition, out Vector2 windowSize))
			{
				return false;
			}

			WindowCreateInfo windowCreateInfo = new(
				(int)windowPosition.X,
				(int)windowPosition.Y,
				(int)windowSize.X,
				(int)windowSize.Y,
				Settings.WindowState,
				windowTitle);

			try
			{
				VeldridStartup.CreateWindowAndGraphicsDevice(
					windowCreateInfo,
					deviceOptions,
					GraphicsBackend.Direct3D11,
					out Sdl2Window window,
					out GraphicsDevice device);

				Device = device;
				if (windowService.AddWindow(window, out WindowHandle? windowHandle))
				{
					MainWindow = windowHandle;
				}
				return true;
			}
			catch (Exception ex)
			{
				logger.LogException("Failed to create window and graphics device!", ex, LogEntrySeverity.Critical);
				return false;
			}
		}

		// Create just the device:
		try
		{
			Device = GraphicsDevice.CreateD3D11(deviceOptions);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create graphics device!", ex, LogEntrySeverity.Critical);
			return false;
		}
	}

	protected override bool HandleSetGraphicsSettings()
	{
		//TODO
		return true;
	}

	#endregion
}
