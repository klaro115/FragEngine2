using System.Text;

namespace FragEngine.Logging;

/// <summary>
/// A very simple logging service that only outputs to the console.
/// </summary>
public sealed class ConsoleLogger : ILogger
{
	#region Events

	/// <summary>
	/// Event that is triggered whenever the severity of an error or exception exceeds a fatal threshold.
	/// </summary>
	public event Action<LogEntrySeverity>? FatalErrorOccurred;

	#endregion
	#region Fields

	private readonly ConsoleColor normalColor = Console.ForegroundColor;
	private readonly SemaphoreSlim semaphore = new(1, 1);

	private LogEntrySeverity maxErrorSeverityForExit = LogEntrySeverity.Fatal;

	private readonly int[] warningSeverityCounter = new int[severityCount];
	private readonly int[] errorSeverityCounter = new int[severityCount];

	#endregion
	#region Constants

	private const int severityCount = (int)LogEntrySeverity.Fatal + 1;

	#endregion
	#region Properties

	/// <summary>
	/// Gets whether the logger is initialized. If false, the logger has already been shut down.
	/// </summary>
	public bool IsInitialized { get; private set; } = true;
	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets or sets the maximum severity of errors and exceptions before other systems need to be notified.
	/// If an error with this or a higher severity is logged, the <see cref="FatalErrorOccurred"/> event is triggered.
	/// </summary>
	public LogEntrySeverity MaxErrorSeverityForExit
	{
		get => maxErrorSeverityForExit;
		set => maxErrorSeverityForExit = (LogEntrySeverity)Math.Clamp((int)value, (int)LogEntrySeverity.Trivial, (int)LogEntrySeverity.Fatal);
	}

	#endregion
	#region Constructors

	~ConsoleLogger()
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
		if (IsInitialized)
		{
			Shutdown();
		}

		IsDisposed = true;
		semaphore.Dispose();
	}

	public void Shutdown()
	{
		IsInitialized = false;

		StringBuilder builder = new();

		int totalWarningCount = warningSeverityCounter.Sum();
		int totalErrorCount = errorSeverityCounter.Sum();

		builder.AppendLine("# Logger Report:");
		if (totalWarningCount > 0)
		{
			builder.AppendLine($"  + Warnings: {totalWarningCount}x");
			for (int i = 0; i < warningSeverityCounter.Length; i++)
			{
				LogEntrySeverity severity = (LogEntrySeverity)i;
				int entryCount = warningSeverityCounter[i];
				builder.AppendLine($"    - {severity}: {entryCount}x");
			}
		}
		if (totalErrorCount > 0)
		{
			builder.AppendLine($"  + Errors: {totalErrorCount}x");
			for (int i = 0; i < errorSeverityCounter.Length; i++)
			{
				LogEntrySeverity severity = (LogEntrySeverity)i;
				int entryCount = errorSeverityCounter[i];
				builder.AppendLine($"    - {severity}: {entryCount}x");
			}
		}
		if (totalErrorCount == 0 && totalWarningCount == 0)
		{
			builder.AppendLine($"  + No errors or warnings.");
		}

		LogMessage(builder.ToString());
	}

	public void LogMessage(string _messageText)
	{
		string formattedMessage = $"[{DateTime.Now:G}] Message: {_messageText}";
		Log(formattedMessage, normalColor);
	}

	public void LogStatus(string _messageText)
	{
		string formattedMessage = $"[{DateTime.Now:G}] Status: {_messageText}";
		Log(formattedMessage, ConsoleColor.Green);
	}

	public void LogWarning(string _messageText, LogEntrySeverity _severity = LogEntrySeverity.Normal)
	{
		string formattedMessage = $"[{DateTime.Now:G}] WARNING, {_severity}: {_messageText}";
		Log(formattedMessage, ConsoleColor.Yellow);

		CountSeverities(warningSeverityCounter, _severity);
	}

	public void LogError(string _messageText, LogEntrySeverity _severity = LogEntrySeverity.Normal)
	{
		string formattedMessage = $"[{DateTime.Now:G}] ERROR, {_severity}: {_messageText}";
		Log(formattedMessage, ConsoleColor.Red);

		CountSeverities(errorSeverityCounter, _severity);
		CheckIfFatalErrorOccurred(_severity);
	}

	public void LogException(string? _messageText, Exception? _exception, LogEntrySeverity _severity = LogEntrySeverity.High)
	{
		string formattedMessage;

		if (!string.IsNullOrEmpty(_messageText) && _exception is not null)
		{
			formattedMessage = $"[{DateTime.Now:G}] EXCEPTION, {_severity}: Error: '{_messageText}' | Exception type: {_exception.GetType().Name} | Exception message: '{_exception.Message}'";
		}
		else if (!string.IsNullOrEmpty(_messageText))
		{
			formattedMessage = $"[{DateTime.Now:G}] EXCEPTION, {_severity}: Error: '{_messageText}'";
		}
		else if (_exception is not null)
		{
			formattedMessage = $"[{DateTime.Now:G}] EXCEPTION, {_severity}: Exception type: {_exception.GetType().Name} | Exception message: '{_exception.Message}'";
		}
		else
		{
			formattedMessage = $"[{DateTime.Now:G}] EXCEPTION, {_severity}: An unspecified exception was thrown.";
		}

		Log(formattedMessage, ConsoleColor.DarkRed);

		CountSeverities(errorSeverityCounter, _severity);
		CheckIfFatalErrorOccurred(_severity);
	}

	private void Log(string _formattedMessage, ConsoleColor _color)
	{
		if (IsDisposed) return;

		semaphore.Wait();

		ConsoleColor prevColor = Console.ForegroundColor;
		Console.ForegroundColor = _color;
		Console.WriteLine(_formattedMessage);
		Console.ForegroundColor = prevColor;

		semaphore.Release();
	}

	private static void CountSeverities(int[] _countersArray, LogEntrySeverity _severity)
	{
		int counterIdx = Math.Clamp((int)_severity, (int)LogEntrySeverity.Trivial, (int)LogEntrySeverity.Fatal);
		_countersArray[counterIdx]++;
	}

	private void CheckIfFatalErrorOccurred(LogEntrySeverity _severity)
	{
		if (_severity >= maxErrorSeverityForExit)
		{
			FatalErrorOccurred?.Invoke(_severity);
		}
	}

	public bool WriteToFile()
	{
		Log("Writing logs to file.", ConsoleColor.Blue);
		return true;
	}

	#endregion
}
