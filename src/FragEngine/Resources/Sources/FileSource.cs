using FragEngine.EngineCore;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Sources;

/// <summary>
/// A resource source that loads resource data from file.
/// </summary>
/// <remarks>
/// This source expects resource files to be located within the app's root resources directory.
/// String-based Source keys are used to identify resources; the format of the source key is a
/// relative file path, starting from the engine's root resource directory.
/// </remarks>
public sealed class FileSource : IResourceSource
{
	#region Fields

	private readonly ILogger logger;

	public readonly string resourcesRootDir;

	#endregion
	#region Constants

	private const string resourceRootDirName = "resources";

	#endregion
	#region Properties

	public bool IsDisposed => false;

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new file source.
	/// </summary>
	/// <param name="_logger">The engine's logging service singleton.</param>
	/// <param name="_platformService">The engine's platform service singleton.</param>
	public FileSource(ILogger _logger, PlatformService _platformService)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_platformService);

		logger = _logger;

		resourcesRootDir = Path.Combine(_platformService.rootDirectoryPath, resourceRootDirName);
	}

	#endregion
	#region Methods

	public void Dispose() { }

	public bool IsValid()
	{
		bool isValid = Directory.Exists(resourcesRootDir);
		return isValid;
	}

	private bool GetResourceFilePath(string? _sourceKey, out string _outFilePath)
	{
		if (string.IsNullOrWhiteSpace(_sourceKey))
		{
			_outFilePath = string.Empty;
			return false;
		}

		_outFilePath = Path.Combine(resourcesRootDir, _sourceKey);
		return true;
	}

	public bool CheckIfResourceExists(string? _sourceKey, int _sourceId)
	{
		if (!GetResourceFilePath(_sourceKey, out string filePath))
		{
			return false;
		}

		try
		{
			return File.Exists(filePath);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool OpenResourceStream(string? _sourceKey, int _sourceId, [NotNullWhen(true)] out Stream? _outStream)
	{
		if (!GetResourceFilePath(_sourceKey, out string filePath))
		{
			logger.LogError("Cannot open resource file stream from source key that is null, empty, or invalid!");
			_outStream = null;
			return false;
		}

		try
		{
			_outStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return true;
		}
		catch (FileNotFoundException ex)
		{
			logger.LogException($"Resource file for resource '{_sourceKey}' does not exist!", ex, LogEntrySeverity.Normal);
			_outStream = null;
			return false;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to open resource stream for resource '{_sourceKey}'!", ex, LogEntrySeverity.Normal);
			_outStream = null;
			return false;
		}
	}

	#endregion
}
