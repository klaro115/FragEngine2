using FragEngine.EngineCore;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using FragEngine.Resources.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace FragEngine.Resources.Internal;

/// <summary>
/// Service for locating resource manifests, and for mapping and accessing raw resource data for loading.
/// </summary>
internal sealed class ResourceDataService : IExtendedDisposable
{
	#region Fields

	private readonly ILogger logger;
	private readonly RuntimeService runtimeService;
	private readonly PlatformService platformService;
	private readonly SerializerService serializerService;

	private readonly ConcurrentDictionary<string, ResourceData> allResourceData = new(-1, ResourceConstants.allResourcesStartingCapacity);

	private readonly ReaderWriterLockSlim resourceDataLock = new();

	#endregion
	#region Constants

	private const int semaphoreWaitTimeoutMs = 100;

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
	public ResourceDataService(
		ILogger _logger,
		RuntimeService _runtimeService,
		PlatformService _platformService,
		SerializerService _serializerService)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_runtimeService);
		ArgumentNullException.ThrowIfNull(_platformService);
		ArgumentNullException.ThrowIfNull(_serializerService);

		logger = _logger;
		runtimeService = _runtimeService;
		platformService = _platformService;
		serializerService = _serializerService;

		//TODO: Create assets root directory if missing!
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

		Debug.Assert(!IsDisposed, $"{nameof(ResourceDataService)} has already been disposed!");

		if (!resourceDataLock.TryEnterReadLock(semaphoreWaitTimeoutMs))
		{
			logger.LogError("Failed to apply resource scan results; waiting for write-lock timed out!", LogEntrySeverity.High);
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
	/// Tries to (re-)scan all asset sources for an up-to-date map of all asset/resource files.
	/// This will search for resource manifest files in both embedded assembly files, and in the app's assets directory.
	/// </summary>
	/// <returns>True if the resource data map was updated, false on error.</returns>
	/// <exception cref="ObjectDisposedException">Resource data service has already been disposed.</exception>
	/// <exception cref="ArgumentNullException">Entry or engine assembly references were null, or file streams were null.</exception>
	/// <exception cref="IOException">Failure to open or read resource manifest files.</exception>
	internal bool ScanForAllResourceData()
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		int newCapacity = Math.Max(allResourceData.Count, ResourceConstants.allResourcesStartingCapacity);
		Dictionary<string, ResourceData> newData = new(newCapacity);

		// Try loading all embedded resources embedded in assemblies:
		if (!GetEmbeddedResourceData(runtimeService.EngineAssembly, newData))
		{
			logger.LogError("Failed to scan assets in embedded resources of engine assembly!");
			return false;
		}

		if (!GetEmbeddedResourceData(runtimeService.EntryAssembly, newData))
		{
			logger.LogError("Failed to scan assets in embedded resources of app's entry assembly!");
			return false;
		}

		// Try loading all file-based resources from assets directory:
		if (!GetAssetFileResourceData(newData))
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
			return true;
		}
		finally
		{
			resourceDataLock.ExitWriteLock();
		}
	}

	private bool GetEmbeddedResourceData(in Assembly _assembly, Dictionary<string, ResourceData> _dstData)
	{
		ArgumentNullException.ThrowIfNull(_assembly);

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
				stream = _assembly.GetManifestResourceStream(resourceName)!;

				if (!ReadResourceManifestFromStream(in stream, _dstData))
				{
					logger.LogError($"Failed to read resource manifest from embedded file! (File: '{resourceName}', Assembly: '{_assembly.GetName().Name}')");
					return false;
				}
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

		return true;
	}

	private bool GetAssetFileResourceData(Dictionary<string, ResourceData> _dstData)
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

				if (!ReadResourceManifestFromStream(in stream, _dstData))
				{
					logger.LogError($"Failed to read resource manifest from embedded file! (File path: '{manifestFilePath}'");
					return false;
				}
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

	private bool ReadResourceManifestFromStream(in Stream _stream, Dictionary<string, ResourceData> _dstData)
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
			if (!_dstData.TryAdd(data.ResourceKey, data))
			{
				logger.LogWarning($"A resource with key '{data.ResourceKey}' has already been registered.", LogEntrySeverity.Trivial);
			}
		}

		return true;
	}

	#endregion
}
