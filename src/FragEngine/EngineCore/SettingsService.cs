using FragEngine.EngineCore.Time;
using FragEngine.EngineCore.Windows;
using FragEngine.Graphics;
using FragEngine.Graphics.Settings;
using FragEngine.Logging;
using FragEngine.Resources.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;

namespace FragEngine.EngineCore;

/// <summary>
/// Helper service for applying settings to various engine services.
/// </summary>
/// <remarks>
/// Some settings will impact more than one service; while you can call each service individually to apply changes
/// to a set of settings, it is generally safer to go through this service, to ensure are consumers of a setting are
/// notified in the correct order.
/// </remarks>
/// <param name="_serviceProvider">The engine's service provider.</param>
/// <param name="_logger">The engine's logging service.</param>
/// <param name="_platformService">The engine's platform service.</param>
/// <param name="_serializerService">The engine's data serialization service.</param>
/// <exception cref="ArgumentNullException">Engine services may not be null.</exception>
public sealed class SettingsService(
	IServiceProvider _serviceProvider,
	ILogger _logger,
	PlatformService _platformService,
	SerializerService _serializerService)
{
	#region Fields

	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
	private readonly IServiceProvider serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
	private readonly PlatformService platformService = _platformService ?? throw new ArgumentNullException(nameof(_platformService));
	private readonly SerializerService serializerService = _serializerService ?? throw new ArgumentNullException(nameof(_serializerService));

	private TimeService? timeService = null;
	private WindowService? windowService = null;
	private GraphicsService? graphicsService = null;

	#endregion
	#region Methods

	/// <summary>
	/// Tries to apply changes in graphics settings across all engine services.
	/// </summary>
	/// <param name="_newGraphicsSettings">The new settings.</param>
	/// <returns>True if the settings were applied successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Graphics settings may not be null.</exception>
	public bool SetGraphicsSettings(in GraphicsSettings _newGraphicsSettings)
	{
		ArgumentNullException.ThrowIfNull(_newGraphicsSettings);

		if (!_newGraphicsSettings.IsValid())
		{
			logger.LogError("Cannot apply invalid graphics settings!");
			return false;
		}

		timeService ??= serviceProvider.GetRequiredService<TimeService>();
		graphicsService ??= serviceProvider.GetRequiredService<GraphicsService>();

		bool success = true;

		timeService.TargetFrameRate = _newGraphicsSettings.FrameRateLimit;
		success &= graphicsService.SetGraphicsSettings(in _newGraphicsSettings);
		//...

		return success;
	}

	/// <summary>
	/// Tries to apply changes in display settings across all engine services.
	/// </summary>
	/// <param name="_newDisplaySettings">The new settings.</param>
	/// <returns>True if the settings were applied successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Display settings may not be null.</exception>
	public bool SetDisplaySettings(in DisplaySettings _newDisplaySettings)
	{
		ArgumentNullException.ThrowIfNull(_newDisplaySettings);

		if (!_newDisplaySettings.IsValid())
		{
			logger.LogError("Cannot apply invalid display settings!");
			return false;
		}

		windowService ??= serviceProvider.GetRequiredService<WindowService>();

		bool success = true;

		success &= windowService.SetDisplaySettings(in _newDisplaySettings);
		//...

		return success;
	}

	/// <summary>
	/// Tries to load a JSON file containing settings from within the engine's settings directory.
	/// </summary>
	/// <remarks>
	/// Note: When loading of a settings file fails, callers are expected to fall back to default values instead.
	/// </remarks>
	/// <typeparam name="T">The type of the data the JSON can be deserialized to.</typeparam>
	/// <param name="_relativeSettingsFilePath">A path to the settings JSON file, relative to the engine's settings directory '<see cref="settingsDirectoryPath"/>'.</param>
	/// <param name="_typeInfo">Type information for deserializing the settings object from JSON.</param>
	/// <param name="_outSettings">Outputs the loaded settings object. This may be null if the JSON contains a null object. Null on error.</param>
	/// <returns>True if the settings object was successfully loaded from file, false otherwise.</returns>
	public bool LoadSettingsFromJsonFile<T>(string _relativeSettingsFilePath, in JsonTypeInfo<T> _typeInfo, out T? _outSettings) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(_relativeSettingsFilePath);
		ArgumentNullException.ThrowIfNull(_typeInfo);

		string settingsPath = Path.Combine(platformService.settingsDirectoryPath, _relativeSettingsFilePath);

		// If settings file does not exit, use default values:
		if (!File.Exists(settingsPath))
		{
			logger.LogWarning("Settings JSON does not exit, using default values instead.", LogEntrySeverity.Trivial);
			_outSettings = null;
			return false;
		}

		// Load settings from file:
		FileStream? fileStream = null;
		try
		{
			fileStream = File.Open(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read);

			if (!serializerService.DeserializeFromJson(fileStream, _typeInfo, out _outSettings))
			{
				logger.LogWarning("Failed to load settings from JSON file, using default values instead.");
				_outSettings = null;
				return false;
			}
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to load settings from JSON file, using default values instead.", ex);
			_outSettings = null;
			return false;
		}
		finally
		{
			fileStream?.Close();
		}

		return true;
	}

	#endregion
}
