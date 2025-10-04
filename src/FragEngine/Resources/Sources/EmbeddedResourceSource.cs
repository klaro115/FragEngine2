using FragEngine.EngineCore;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FragEngine.Resources.Sources;

/// <summary>
/// A resource source that loads resource data from an assembly's embedded resources.
/// </summary>
public sealed class EmbeddedResourceSource : IResourceSource
{
	#region Fields

	private readonly ILogger logger;
	private readonly Assembly entryAssembly;

	#endregion
	#region Properties

	public bool IsDisposed => false;

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new file source for embedded resources.
	/// </summary>
	/// <param name="_logger">The engine's logging service singleton.</param>
	/// <param name="_runtimeService">The engine's runtime service singleton.</param>
	public EmbeddedResourceSource(ILogger _logger, RuntimeService _runtimeService)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_runtimeService);

		logger = _logger;
		entryAssembly = _runtimeService.EntryAssembly;
	}

	#endregion
	#region Methods

	public void Dispose() { }

	public bool IsValid() => true;

	public bool CheckIfResourceExists(string? _sourceKey, int _sourceId)
	{
		if (string.IsNullOrWhiteSpace(_sourceKey))
		{
			return false;
		}

		try
		{
			string[] allResourceNames = entryAssembly.GetManifestResourceNames();
			bool embeddedFileExists = allResourceNames.Contains(_sourceKey);
			return embeddedFileExists;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool OpenResourceStream(string? _sourceKey, int _sourceId, [NotNullWhen(true)] out Stream? _outStream)
	{
		if (string.IsNullOrWhiteSpace(_sourceKey))
		{
			_outStream = null;
			return false;
		}

		try
		{
			_outStream = entryAssembly.GetManifestResourceStream(_sourceKey);
			if (_outStream is null)
			{
				logger.LogError($"Failed to open embedded file stream for resource '{_sourceKey}'!", LogEntrySeverity.Normal);
				return false;
			}

			return true;
		}
		catch (FileNotFoundException ex)
		{
			logger.LogException($"Embedded file for resource '{_sourceKey}' does not exist!", ex, LogEntrySeverity.Normal);
			_outStream = null;
			return false;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to open embedded file stream for resource '{_sourceKey}'!", ex, LogEntrySeverity.Normal);
			_outStream = null;
			return false;
		}
	}

	#endregion
}
