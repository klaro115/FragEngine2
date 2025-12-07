using FragEngine.EngineCore;
using FragEngine.Extensions;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using FragEngine.Resources.Serialization;
using FragEngine.Resources.Sources;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace FragEngine.Resources.Internal;

/// <summary>
/// Service for locating resource manifests, and for mapping and accessing raw resource data for loading.
/// </summary>
public sealed class ResourceDataService : IExtendedDisposable
{
	#region Events

	/// <summary>
	/// Event that is triggered when a resource data scan was completed.
	/// </summary>
	public event Action? ResourceDataScanCompleted = null;

	#endregion
	#region Fields

	// Services:
	private readonly ILogger logger;
	private readonly RuntimeService runtimeService;
	private readonly PlatformService platformService;
	private readonly SerializerService serializerService;

	// Resource sources:
	private readonly FileSource fileSource;
	private readonly NetworkSource networkSource;
	private readonly Dictionary<string, EmbeddedResourceSource> allEmbeddedSources = new(2);

	// Data:
	private readonly ConcurrentDictionary<string, ResourceData> allResourceData = new(-1, ResourceConstants.allResourcesStartingCapacity);

	private readonly ReaderWriterLockSlim resourceDataLock = new(LockRecursionPolicy.SupportsRecursion);

	#endregion
	#region Constants

	private const int semaphoreWaitTimeoutMs = 100;

	private const string embeddedPathPartSeparator = "::";

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new resource data service.
	/// </summary>
	/// <param name="_logger">The engine's logging service.</param>
	/// <param name="_runtimeService">The engine's runtime information service.</param>
	/// <param name="_platformService">The engine's platform management service.</param>
	/// <param name="_serializerService">The engine's data serialization service.</param>
	/// <exception cref="ArgumentNullException">Engine services may not be null.</exception>
	/// <exception cref="Exception">Failed to prepare assets directories.</exception>
	public ResourceDataService(
		ILogger _logger,
		RuntimeService _runtimeService,
		PlatformService _platformService,
		SerializerService _serializerService,
		FileSource _fileSource,
		EmbeddedResourceSource _embeddedResourceSource,
		NetworkSource _networkSource)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_runtimeService);
		ArgumentNullException.ThrowIfNull(_platformService);
		ArgumentNullException.ThrowIfNull(_serializerService);
		ArgumentNullException.ThrowIfNull(_fileSource);
		ArgumentNullException.ThrowIfNull(_embeddedResourceSource);
		ArgumentNullException.ThrowIfNull(_networkSource);

		logger = _logger;
		runtimeService = _runtimeService;
		platformService = _platformService;
		serializerService = _serializerService;
		fileSource = _fileSource;
		networkSource = _networkSource;

		allEmbeddedSources.Add(runtimeService.EntryAssembly.GetName().Name!, _embeddedResourceSource);

		logger.LogStatus("# Initializing resource data service.");

		if (!CreateRootDirectories())
		{
			throw new Exception("Failed to prepare asset root directories!");
		}

		string truncatedDirectoryPath = platformService.assetDirectoryPath.TruncateWithEllipsis(64, "...", StringExt.TruncationType.Start);
		logger.LogMessage($"- Assets directory: {truncatedDirectoryPath}");
	}

	~ResourceDataService()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}

	private void Dispose(bool _)
	{
		IsDisposed = true;

		resourceDataLock.Dispose();
	}

	private bool CreateRootDirectories()
	{
		if (!Directory.Exists(platformService.rootDirectoryPath))
		{
			logger.LogError("Application root directory does not exist!");
			return false;
		}

		try
		{
			if (!Directory.Exists(platformService.assetDirectoryPath))
			{
				Directory.CreateDirectory(platformService.assetDirectoryPath);
			}
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create assets directory!", ex, LogEntrySeverity.Critical);
			return false;
		}
	}

	/// <summary>
	/// Tries to retrieve resource file data for a specific resource.
	/// </summary>
	/// <param name="_resourceKey">A unique identifier for the resource.</param>
	/// <param name="_outData">Outputs resource data describing the location of the resource's importable data. Null on error.</param>
	/// <returns>True if the resource is known and its data was found, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource key may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Resource data service has already been disposed.</exception>
	internal bool GetResourceData(string _resourceKey, [NotNullWhen(true)] out ResourceData? _outData)
	{
		ArgumentNullException.ThrowIfNull(_resourceKey);
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		if (!resourceDataLock.TryEnterReadLock(semaphoreWaitTimeoutMs))
		{
			logger.LogError("Failed to get resource file data; waiting for read-lock timed out!", LogEntrySeverity.High);
			_outData = null;
			return false;
		}
		try
		{
			return allResourceData.TryGetValue(_resourceKey, out _outData);
		}
		finally
		{
			resourceDataLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Tries to retrieve all resource file data.
	/// </summary>
	/// <param name="_outAllResourceData">Outputs a read-only dictionary of all resources that were identified during the last scan. Null on error.</param>
	/// <returns>True if the resource data query succeeded, false otherwise.</returns>
	/// <exception cref="ObjectDisposedException">Resource data service has already been disposed.</exception>
	internal bool GetAllResourceData([NotNullWhen(true)] out IReadOnlyDictionary<string, ResourceData>? _outAllResourceData)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		if (!resourceDataLock.TryEnterReadLock(semaphoreWaitTimeoutMs))
		{
			logger.LogError("Failed to get all resource file data; waiting for read-lock timed out!", LogEntrySeverity.High);
			_outAllResourceData = null;
			return false;
		}
		try
		{
			_outAllResourceData = allResourceData;
			return true;
		}
		finally
		{
			resourceDataLock.ExitReadLock();
		}
	}

	/// <summary>
	/// Tries to open a stream from which a resource's data may be imported.
	/// </summary>
	/// <param name="_data">The resource whose data we wish to import. Procedurally generated resources are not supported. May not be null.</param>
	/// <param name="_outResourceStream">Outputs a stream from which the resource's data may be imported. The stream's read position will already be
	/// moved to the resource's location within the stream's underlying data. Null if the resource could not be found or if it was invalid.<para/>
	/// WARNING: Ownership of this stream is transferred to the caller! Remember to close the stream ASAP once you have finished importing data from
	/// it. Failure to close a resource data stream may block further access to files or network ports pointing at the same source location.</param>
	/// <returns>True if a resource stream could be opened, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource data may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Resource data service has already been disposed.</exception>
	public bool OpenResourceStream(in ResourceData _data, [NotNullWhen(true)] out Stream? _outResourceStream)
	{
		ArgumentNullException.ThrowIfNull(_data);
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		// Check superficial validity of data:
		if (!_data.IsValid())
		{
			logger.LogError($"Cannot open resource data stream; resource data for key '{_data.ResourceKey}' is invalid!");
			_outResourceStream = null;
			return false;
		}

		// Abort if the resource is not supported on the current platform:
		if (_data.OSRestriction is not null && _data.OSRestriction != platformService.OperatingSystem)
		{
			logger.LogError($"Cannot open resource data stream; resource data for key '{_data.ResourceKey}' is restricted to OS '{_data.OSRestriction}'!");
			_outResourceStream = null;
			return false;
		}
		if (_data.GraphicsRestriction is not null && _data.GraphicsRestriction != platformService.GraphicsBackend)
		{
			logger.LogError($"Cannot open resource data stream; resource data for key '{_data.ResourceKey}' is restricted to graphics API '{_data.GraphicsRestriction}'!");
			_outResourceStream = null;
			return false;
		}

		bool success;

		// Try to locate and access resource data from its source location:
		switch (_data.Location)
		{
			case ResourceLocationType.AssetFile:
				success = OpenResourceStreamFromAssetFile(in _data, out _outResourceStream);
				break;
			case ResourceLocationType.EmbeddedFile:
				success = OpenResourceStreamFromEmbeddedFile(in _data, out _outResourceStream);
				break;
			case ResourceLocationType.Network:
				success = OpenResourceStreamFromNetwork(in _data, out _outResourceStream);
				break;
			case ResourceLocationType.Procedural:
			case ResourceLocationType.Unknown:
			default:
				{
					logger.LogError($"Cannot open resource data stream; resource data for key '{_data.ResourceKey}' uses unsupported location '{_data.Location}'!");
					_outResourceStream = null;
					success = false;
					break;
				}
		}

		return success;
	}

	private bool OpenResourceStreamFromAssetFile(in ResourceData _data, [NotNullWhen(true)] out Stream? _outResourceStream)
	{
		if (!fileSource.CheckIfResourceExists(_data.RelativePath, 0))
		{
			logger.LogError($"Invalid path for asset file resource '{_data.ResourceKey}'! Path: '{_data.RelativePath}'");
			_outResourceStream = null;
			return false;
		}

		return fileSource.OpenResourceStream(_data.RelativePath, 0, out _outResourceStream);
	}

	private bool OpenResourceStreamFromEmbeddedFile(in ResourceData _data, [NotNullWhen(true)] out Stream? _outResourceStream)
	{
		// Identify assembly and relative file path:
		string[] parts = _data.RelativePath.Split(embeddedPathPartSeparator, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length != 2)
		{
			logger.LogError($"Invalid path for embedded resource '{_data.ResourceKey}'! Path: '{_data.RelativePath}'");
			_outResourceStream = null;
			return false;
		}

		if (!allEmbeddedSources.TryGetValue(parts[0], out EmbeddedResourceSource? embeddedSource))
		{
			logger.LogError($"Invalid assembly name for embedded resource '{_data.ResourceKey}'! Assembly: '{parts[0]}'");
			_outResourceStream = null;
			return false;
		}

		return embeddedSource.OpenResourceStream(parts[1], 0, out _outResourceStream);
	}

	private bool OpenResourceStreamFromNetwork(in ResourceData _data, [NotNullWhen(true)] out Stream? _outResourceStream)
	{
		if (!networkSource.CheckIfResourceExists(_data.RelativePath, 0))
		{
			logger.LogError($"Invalid URL for network resource '{_data.ResourceKey}'! URL: '{_data.RelativePath}'");
			_outResourceStream = null;
			return false;
		}

		return networkSource.OpenResourceStream(_data.RelativePath, 0, out _outResourceStream);
	}

	/// <summary>
	/// Tries to (re-)scan all asset sources for an up-to-date map of all asset/resource files.
	/// This will search for resource manifest files in both embedded assembly files, and in the app's assets directory.
	/// </summary>
	/// <returns>True if the resource data map was updated, false on error.</returns>
	/// <exception cref="ArgumentNullException">Entry or engine assembly references were null, or file streams were null.</exception>
	/// <exception cref="IOException">Failure to open or read resource manifest files.</exception>
	/// <exception cref="ObjectDisposedException">Resource data service has already been disposed.</exception>
	internal bool ScanForAllResourceData()
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		logger.LogMessage("Starting resource data scan...");

		int newCapacity = Math.Max(allResourceData.Count, ResourceConstants.allResourcesStartingCapacity);
		Dictionary<string, ResourceData> newData = new(newCapacity);
		int manifestCount = 0;

		// Try loading all embedded resources embedded in assemblies:
		if (!GetResourceDataFromEmbeddedFiles(runtimeService.EngineAssembly, newData, ref manifestCount))
		{
			logger.LogError("Failed to scan assets in embedded resources of engine assembly!");
			return false;
		}

		if (!GetResourceDataFromEmbeddedFiles(runtimeService.EntryAssembly, newData, ref manifestCount))
		{
			logger.LogError("Failed to scan assets in embedded resources of app's entry assembly!");
			return false;
		}

		// Try loading all file-based resources from assets directory:
		if (!GetResourceDataFromAssetsDirectory(newData, ref manifestCount))
		{
			logger.LogError("Failed to scan assets in asset file directory!");
			return false;
		}

		// Replace all existing resource data with the newly scanned data:
		if (!resourceDataLock.TryEnterWriteLock(semaphoreWaitTimeoutMs))
		{
			logger.LogError("Failed to apply resource scan results; waiting for write-lock timed out!", LogEntrySeverity.High);
			return false;
		}
		try
		{
			allResourceData.Clear();
			foreach ((string key, ResourceData data) in newData)
			{
				allResourceData.TryAdd(key, data);
			}

			logger.LogMessage($"Resource data scan complete; Found {allResourceData.Count} keyed resources across {manifestCount} manifests.");
			ResourceDataScanCompleted?.Invoke();
			return true;
		}
		finally
		{
			resourceDataLock.ExitWriteLock();
		}
	}

	private bool GetResourceDataFromEmbeddedFiles(in Assembly _assembly, Dictionary<string, ResourceData> _dstData, ref int _manifestCount)
	{
		ArgumentNullException.ThrowIfNull(_assembly);

		bool hasManifests = false;
		string assemblyName = _assembly.GetName().Name ?? string.Empty;

		string[] allEmbeddedResources = _assembly.GetManifestResourceNames();

		foreach (string resourceName in allEmbeddedResources)
		{
			// Discard all embedded files that do not have the resource manifest file extension:
			if (!resourceName.EndsWith(ResourceConstants.resourceManifestFileExtension, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// Try parsing the manifest file:
			Stream? stream = null;
			try
			{
				string sourcePath = $"{assemblyName}{embeddedPathPartSeparator}";

				stream = _assembly.GetManifestResourceStream(resourceName)!;

				if (!ReadResourceManifestFromStream(in stream, _dstData, sourcePath, ResourceLocationType.EmbeddedFile))
				{
					logger.LogError($"Failed to read resource manifest from embedded file! (File: '{resourceName}', Assembly: '{_assembly.GetName().Name}')");
					return false;
				}

				_manifestCount++;
				hasManifests = true;
			}
			catch (Exception ex)
			{
				logger.LogException($"Failed to read resource manifest from embedded file! (File: '{resourceName}', Assembly: '{_assembly.GetName().Name}')", ex);
				return false;
			}
			finally
			{
				stream?.Close();
			}
		}

		// If the assembly contained embedded manifests, map it for importing data later:
		if (hasManifests && !string.IsNullOrEmpty(assemblyName) && !allEmbeddedSources.ContainsKey(assemblyName))
		{
			EmbeddedResourceSource newEmbeddedSource = new(logger, _assembly);
			allEmbeddedSources.TryAdd(assemblyName, newEmbeddedSource);
		}

		return true;
	}

	private bool GetResourceDataFromAssetsDirectory(Dictionary<string, ResourceData> _dstData, ref int _manifestCount)
	{
		string assetRootDir = platformService.assetDirectoryPath;
		if (!Directory.Exists(assetRootDir))
		{
			logger.LogError($"Assets root directory does not exit! Path: '{assetRootDir}'");
			return false;
		}

		const string manifestFileSearchPattern = $"*{ResourceConstants.resourceManifestFileExtension}";

		IEnumerable<string> manifestFiles = Directory.EnumerateFiles(assetRootDir, manifestFileSearchPattern, SearchOption.AllDirectories);
		foreach (string manifestFilePath in manifestFiles)
		{
			// Try parsing the manifest file:
			Stream? stream = null;
			try
			{
				stream = File.Open(manifestFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

				if (!ReadResourceManifestFromStream(in stream, _dstData, string.Empty, ResourceLocationType.AssetFile))
				{
					logger.LogError($"Failed to read resource manifest from embedded file! (File path: '{manifestFilePath}'");
					return false;
				}

				_manifestCount++;
			}
			catch (Exception ex)
			{
				logger.LogException($"Failed to read resource manifest from embedded file! (File path: '{manifestFilePath}'", ex);
				return false;
			}
			finally
			{
				stream?.Close();
			}
		}

		return true;
	}

	private bool ReadResourceManifestFromStream(in Stream _stream, Dictionary<string, ResourceData> _dstData, string _relativePathPrefix, ResourceLocationType _locationType)
	{
		ArgumentNullException.ThrowIfNull(_stream);

		Debug.Assert(_stream.CanRead, "Resource manifest file stream may not be write-only!");

		// Deserialize manifest JSON from stream:
		JsonTypeInfo<ResourceManifest> typeInfo = ResourceDataJsonContext.Default.ResourceManifest;

		if (!serializerService.DeserializeFromJson(_stream, typeInfo, out ResourceManifest? manifest))
		{
			logger.LogError($"Failed to deserialize resource manifest from stream!");
			return false;
		}
		if (manifest is null || !manifest.IsValid())
		{
			logger.LogError($"Deserialized resource manifest was null or invalid!");
			return false;
		}

		// Quietly skip resources with incompatible platform-restrictions:
		if ((manifest.OSRestriction is not null && manifest.OSRestriction != platformService.OperatingSystem) ||
			(manifest.GraphicsRestriction is not null && manifest.GraphicsRestriction != platformService.GraphicsBackend))
		{
			return true;
		}

		// Add all resources with new keys to the dictionary:
		foreach (ResourceData data in manifest.Resources)
		{
			ResourceData locatedData = data with
			{
				Location = _locationType,
				RelativePath = $"{_relativePathPrefix}{data.RelativePath}",
			};

			if (!_dstData.TryAdd(data.ResourceKey, locatedData))
			{
				logger.LogWarning($"A resource with key '{data.ResourceKey}' has already been registered.", LogEntrySeverity.Trivial);
			}
		}

		return true;
	}

	#endregion
}
