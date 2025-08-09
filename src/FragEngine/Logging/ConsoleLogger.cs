namespace FragEngine.Logging;

/// <summary>
/// A very simple logging service that only outputs to the console.
/// </summary>
public sealed class ConsoleLogger : ILogger, IDisposable
{
	#region Fields

	private readonly ConsoleColor normalColor = Console.ForegroundColor;
	private readonly SemaphoreSlim semaphore = new(1, 1);

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

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
		IsDisposed = true;
		semaphore.Dispose();
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
	}

	public void LogError(string _messageText, LogEntrySeverity _severity = LogEntrySeverity.Normal)
	{
		string formattedMessage = $"[{DateTime.Now:G}] ERROR, {_severity}: {_messageText}";
		Log(formattedMessage, ConsoleColor.Red);
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

	public bool WriteToFile()
	{
		Log("Writing logs to file.", ConsoleColor.Blue);
		return true;
	}

	#endregion
}
