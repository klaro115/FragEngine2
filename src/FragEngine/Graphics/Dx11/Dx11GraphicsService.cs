using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Windows;
using FragEngine.Logging;
using System.Numerics;
using System.Runtime.InteropServices;
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

	internal override bool Initialize(GraphicsServiceInitFlags _initFlags)
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

		if (!CreateGraphicsDevice(_initFlags))
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

		MainWindow?.CloseWindow();
		Device.Dispose();

		IsInitialized = false;
		return true;
	}

	private bool CreateGraphicsDevice(GraphicsServiceInitFlags _initFlags)
	{
		if (!_initFlags.HasFlag(GraphicsServiceInitFlags.CreateDevice))
		{
			logger.LogError($"Initialization flags did not have graphics device flag! (Flags: '{_initFlags}')");
			return false;
		}

		GraphicsDeviceOptions deviceOptions = new(
			config.CreateDebug,
			PixelFormat.D24_UNorm_S8_UInt,
			config.VSync,
			ResourceBindingModel.Improved,
			true,
			true,
			config.OutputIsSRgb);

		// Create with window and swapchain:
		if (_initFlags.HasFlag(GraphicsServiceInitFlags.CreateMainWindowAndSwapchain))
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
				MainSwapchain = device.MainSwapchain;
				ResourceFactory = device.ResourceFactory;

				if (windowService.AddWindow(window, MainSwapchain, out WindowHandle? windowHandle))
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
			ResourceFactory = Device.ResourceFactory;
			MainSwapchain = null;

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

	internal override bool CreateSwapchain(Sdl2Window _window, out Swapchain? _outSwapchain)
	{
		if (_window is null || !_window.Exists)
		{
			logger.LogError("Cannot create swapchain for null or closed SDL2 window!");
			_outSwapchain = null;
			return false;
		}

		IntPtr hInstance = Marshal.GetHINSTANCE(typeof(Dx11GraphicsService).Module);
		SwapchainSource source = SwapchainSource.CreateWin32(_window.Handle, hInstance);

		SwapchainDescription swapchainDesc = new(
			source,
			(uint)_window.Width,
			(uint)_window.Height,
			PixelFormat.D24_UNorm_S8_UInt,
			config.VSync);

		try
		{
			_outSwapchain = ResourceFactory.CreateSwapchain(ref swapchainDesc);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create swapchain for SDL2 window!", ex);
			_outSwapchain = null;
			return false;
		}
	}

	internal override bool Draw()
	{
		if (!IsInitialized)
		{
			logger.LogError("Cannot execute draw calls using uninitialized or disposed graphics service!", LogEntrySeverity.Critical);
			return false;
		}

		if (!PrepareCommandListExecution())
		{
			return false;
		}

		// Submit all command lists to device:
		try
		{
			while (commandListExecutionQueue.TryDequeue(out CommandList? cmdList, out _))
			{
				Device.SubmitCommands(cmdList);
			}
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to submit command lists to graphics device!", ex, LogEntrySeverity.High);
			return false;
		}
		finally
		{
			commandListExecutionQueue.Clear();
		}

		// Present rendering result to screen:
		if (MainSwapchain is not null)
		{
			Device.SwapBuffers();
		}
		return true;
	}

	#endregion
}
