using FragEngine.Application;
using FragEngine.EngineCore.Config;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FragEngine.EngineCore;

/// <summary>
/// Engine service that provides information about the runtime environment.
/// </summary>
public sealed class RuntimeService
{
	#region Fields

	private readonly ILogger logger;
	private readonly EngineConfig config;
	private readonly IServiceProvider serviceProvider;

	#endregion
	#region Properties

	/// <summary>
	/// Gets the entry assembly of the app.
	/// </summary>
	/// <remarks>
	/// For app builds, this will return the executing assembly.
	/// For unit tests, this will return the unit test project instead.<para/>
	/// It is recommended to use this property over '<see cref="Assembly.GetExecutingAssembly()"/>',
	/// because the latter may return null or the test runner in unit test environments.
	/// </remarks>
	public Assembly EntryAssembly { get; }

	/// <summary>
	/// Gets the assembly containing the core engine modules and types.
	/// </summary>
	public Assembly EngineAssembly { get; }

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new runtime service instance.
	/// </summary>
	/// <param name="_logger">The logging service singleton.</param>
	/// <param name="_config">The engine configuration.</param>
	/// <param name="_serviceProvider">The engine's DI service provider.</param>
	/// <exception cref="ArgumentNullException">Logger, engine config, and service provider may not be null.</exception>
	/// <exception cref="Exception">Failure to determine the app's entry assembly.</exception>
	public RuntimeService(ILogger _logger, EngineConfig _config, IServiceProvider _serviceProvider)
	{
		logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
		config = _config ?? throw new ArgumentNullException(nameof(_config));
		serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));

		logger.LogStatus("# Initializing runtime service.");

		// Entry assembly:
		if (!GetEntryAssembly(out Assembly? entryAssembly))
		{
			throw new Exception("Failed to identify the app's entry assembly!");
		}
		EntryAssembly = entryAssembly;
		EngineAssembly = typeof(Engine).Assembly;

		logger.LogMessage($"- Entry assembly: {EntryAssembly}");
	}

	#endregion
	#region Methods

	private bool GetEntryAssembly([NotNullWhen(true)] out Assembly? _outEntryAssembly)
	{
		// If available, derive entry assembly from app logic implementation type:
		if (config.Startup.AddAppLogicToServiceProvider)
		{
			IAppLogic? appLogic = serviceProvider.GetService<IAppLogic>();
			if (appLogic is not null)
			{
				_outEntryAssembly = appLogic.GetType().Assembly;
				return true;
			}
		}

		// See if an override entry assembly was provided:
		_outEntryAssembly = Assembly.GetEntryAssembly();
		if (_outEntryAssembly is not null)
		{
			return true;
		}

		// Last resort: use executing assembly:
		_outEntryAssembly = Assembly.GetExecutingAssembly();
		return _outEntryAssembly is not null;
	}

	#endregion
}
