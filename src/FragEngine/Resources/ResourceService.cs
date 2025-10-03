using FragEngine.EngineCore;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources;

public sealed class ResourceService : IExtendedDisposable
{
	#region Fields

	private readonly ILogger logger;
	private readonly PlatformService platformService;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	#endregion
	#region Constructors

	public ResourceService(ILogger _logger, PlatformService _platformService)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_platformService);

		logger = _logger;
		platformService = _platformService;

		logger.LogStatus("# Initializing resource service.");

		//...
	}

	~ResourceService()
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

		//...
	}

	internal bool LoadResource(ResourceHandle _handle, bool _loadImmediately, FuncAssignLoadedResource _funcAssignResourceCallback)
	{
		ArgumentNullException.ThrowIfNull(_handle);
		ArgumentNullException.ThrowIfNull(_funcAssignResourceCallback);

		if (IsDisposed)
		{
			logger.LogError("Cannot load resource using disposed resource service!");
			return false;
		}
		if (!_handle.IsValid())
		{
			return false;
		}

		//TODO

		return false;	//TEMP
	}

	internal async Task<bool> LoadResourceAsync(ResourceHandle _handle, FuncAssignLoadedResource _funcAssignResourceCallback)
	{
		ArgumentNullException.ThrowIfNull(_handle);
		ArgumentNullException.ThrowIfNull(_funcAssignResourceCallback);

		if (IsDisposed)
		{
			logger.LogError("Cannot load resource asynchronously using disposed resource service!");
			return false;
		}
		if (!_handle.IsValid())
		{
			return false;
		}

		//TODO

		return false;   //TEMP
	}

	public bool AbortLoading(ResourceHandle _handle)
	{
		ArgumentNullException.ThrowIfNull(_handle);

		if (IsDisposed)
		{
			logger.LogError("Cannot abort of resource resource service that has already been disposed!");
			return false;
		}

		//TODO

		return false;	//TEMP
	}

	internal bool GetResourceData(string _resourceKey, [NotNullWhen(true)] out ResourceData? _outData)
	{
		ArgumentNullException.ThrowIfNull(_resourceKey);

		if (IsDisposed)
		{
			logger.LogError("Cannot get resource data using disposed resource service!");
			_outData = null;
			return false;
		}

		//TODO

		_outData = null;	//TEMP
		return false;
	}

	#endregion
}
