using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.EngineCore.Windows.Linux;
using FragEngine.Extensions.SDL;
using FragEngine.Helpers;
using FragEngine.Logging;
using FragEngine.Resources.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Vortice;

namespace FragEngine.Graphics.Vulkan;

/// <summary>
/// Graphics service implementation for the Vulkan graphics API.
/// </summary>
/// <param name="_logger">The logger service.</param>
internal sealed class VulkanGraphicsService(
	ILogger _logger,
	PlatformService _platformService,
	WindowService _windowService,
	TimeService _timeService,
	SerializerService _serializerService,
	EngineConfig _config)
	: GraphicsService(_logger, _platformService, _windowService, _timeService, _serializerService, _config)
{
	#region Methods

	internal override bool Initialize(GraphicsServiceInitFlags _initFlags)
	{
		if (IsDisposed)
		{
			logger.LogError($"Cannot initialize {nameof(VulkanGraphicsService)} that has already been disposed.", LogEntrySeverity.High);
			return false;
		}
		if (IsInitialized)
		{
			logger.LogWarning($"{nameof(VulkanGraphicsService)} is already initialized.");
			return true;
		}

		if (!CreateGraphicsDevice(_initFlags))
		{
			return false;
		}

		LogDeviceDetails();

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
					GraphicsBackend.Vulkan,
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
			Device = GraphicsDevice.CreateVulkan(deviceOptions);
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

	internal override bool CreateSwapchain(Sdl2Window _window, [NotNullWhen(true)] out Swapchain? _outSwapchain)
	{
		if (_window is null || !_window.Exists)
		{
			logger.LogError("Cannot create swapchain for null or closed SDL2 window!");
			_outSwapchain = null;
			return false;
		}

		if (!CreateSwapchainSource(_window, out SwapchainSource? source))
		{
			logger.LogError("Failed create swapchain source for SDL2 window!");
		}

		// Create swapchain:
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

	private bool CreateSwapchainSource(Sdl2Window _window, [NotNullWhen(true)] out SwapchainSource? _outSource)
	{
		if (OperatingSystem.IsWindows())
		{
			// Determine the app's instance handle:
			if (!WindowsHelper.TryGetAppHInstance(logger, _window, out nint hInstance))
			{
				logger.LogError("Cannot determine the app's HInstance; unable to create Dx11/Win32 swapchain!");
				_outSource = null;
				return false;
			}

			// Create a Win32 swapchain source:
			_outSource = SwapchainSource.CreateWin32(_window.Handle, hInstance);
		}
		else if (OperatingSystem.IsLinux())
		{
			if (!_window.TryGetWaylandInfo(logger, out LinuxWaylandWMInfo waylandInfo))
			{
				logger.LogError("Cannot determine the window's handles; unable to create Wayland/Linux swapchain!");
				_outSource = null;
				return false;
			}

			// Create a Linux/Wayland swapchain source:
			_outSource = SwapchainSource.CreateWayland(waylandInfo.display, waylandInfo.surface);

			//TODO [later]: Add support for X11 or other Linux window managers.
		}
		else
		{
			logger.LogError("OS platform not supported or not implemented; unable to create swapchain source!");
			_outSource = null;
			return false;
		}

		return true;
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
			OnMainSwapchainSwapped();
		}
		return true;
	}

	protected override bool LogDeviceDetails()
	{
		GraphicsDeviceFeatures features = Device.Features;
		List<string> logLines = new(10)
		{
			// Device:
			 "+ Graphics Device:",
			$"  - Name:                {Device.DeviceName}",
			$"  - Vendor ID:           {Device.VendorName}",
			$"  - API:                 {Device.BackendType}",
			$"  - API version:         {Device.ApiVersion}",

			// Features:
			 "+ GPU Features:",
			$"  - Compute Shader:      {features.ComputeShader}",
			$"  - Geometry Shader:     {features.GeometryShader}",
			$"  - Tesselation Shader:  {features.TessellationShaders}",
			$"  - Draw Indirect:       {features.DrawIndirect}",
			$"  - Structured Buffer:   {features.StructuredBuffer}",
			$"  - Texture1D:           {features.Texture1D}",
			$"  - Shader Float64:      {features.ShaderFloat64}",
			//...
		};

		// Query Vulkan-specific details:
		if (!Device.GetVulkanInfo(out BackendInfoVulkan info))
		{
			logger.LogMessages(logLines);
			logger.LogWarning("Failed to query Vulkan-specific graphics device features!");
			return false;
		}

		ReadOnlyCollection<string> instanceLayers = info.AvailableInstanceLayers;
		ReadOnlyCollection<string> instanceExtensions = info.AvailableInstanceExtensions;

		logLines.Add("+ Vulkan Features:");
		logLines.Add($"  - Driver Name:         {info.DriverName}");
		logLines.Add($"  - Driver Info:         {info.DriverInfo}");
		logLines.Add($"  - Device Extensions:   {info.AvailableDeviceExtensions.Count}x");
		logLines.Add($"  - Instance Layers:     {instanceLayers.Count}x");
		for (int i = 0; i < instanceLayers.Count; i++)
		{
			logLines.Add($"    * Layer {i,2}:          {instanceLayers[i]}x");
		}
		logLines.Add($"  - Instance Extensions: {instanceExtensions.Count}x");
		for (int i = 0; i < instanceExtensions.Count; i++)
		{
			logLines.Add($"    * Extension {i,2}:      {instanceExtensions[i]}x");
		}

		// Log all lines as one uninterrupted block:
		logger.LogMessages(logLines);
		return true;
	}

	#endregion
}
