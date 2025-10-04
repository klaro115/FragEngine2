using FragEngine.EngineCore;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Internal;

internal sealed class ResourceDataService(ILogger _logger, PlatformService _platformService)
{
	#region Fields

	private readonly ILogger logger = _logger;
	private readonly PlatformService platformService = _platformService;

	private readonly ConcurrentDictionary<string, ResourceData> allResourceData = new(-1, ResourceConstants.allResourcesStartingCapacity);

	#endregion
	#region Methods

	//TODO

	internal bool GetResourceData(string _resourceKey, [NotNullWhen(true)] out ResourceData? _outData)
	{
		ArgumentNullException.ThrowIfNull(_resourceKey);

		return allResourceData.TryGetValue(_resourceKey, out _outData);
	}

	#endregion
}
