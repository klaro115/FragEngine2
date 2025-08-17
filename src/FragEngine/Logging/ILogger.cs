namespace FragEngine.Logging;

/// <summary>
/// Interface for logging services. Errors or console messages will be written to their respective outputs using an implementation of this service.
/// </summary>
public interface ILogger
{
	#region Properties

	/// <summary>
	/// Gets whether this object has already been disposed.
	/// </summary>
	bool IsDisposed { get; }

	#endregion
	#region Methods

	/// <summary>
	/// Shuts the logger down safely.
	/// </summary>
	/// <remarks>
	/// When this method is called, it may be a good time to format and log a final report, such as error statistics.
	/// </remarks>
	void Shutdown();

	/// <summary>
	/// Logs a message or status update.
	/// </summary>
	/// <param name="_messageText">The content of the message.</param>
	void LogMessage(string _messageText);

	/// <summary>
	/// Logs a major status update.
	/// </summary>
	/// <param name="_messageText">The content of the message.</param>
	void LogStatus(string _messageText);

	/// <summary>
	/// Logs a warning message.
	/// </summary>
	/// <param name="_messageText">The content of the error message.</param>
	/// <param name="_severity">Severity of the warning.</param>
	void LogWarning(string _messageText, LogEntrySeverity _severity = LogEntrySeverity.Normal);

	/// <summary>
	/// Logs an error message.
	/// </summary>
	/// <param name="_messageText">The content of the error message.</param>
	/// <param name="_severity">Severity of the error.</param>
	void LogError(string _messageText, LogEntrySeverity _severity = LogEntrySeverity.Normal);

	/// <summary>
	/// Logs an exception that was caught.
	/// </summary>
	/// <param name="_messageText">The content of the accompanying error message. If null, only the exception body is logged.</param>
	/// <param name="_exception">The exception that was caught. If null, only the message text is logged.</param>
	/// <param name="_severity">Severity of the exception.</param>
	void LogException(string? _messageText, Exception? _exception, LogEntrySeverity _severity = LogEntrySeverity.High);

	/// <summary>
	/// Write any pending or queued log entries to file immediately. Do nothing if all entries have already been writting to their final output.
	/// </summary>
	/// <returns>True if any pending logs were written to destination, false on error.</returns>
	bool WriteToFile();

	#endregion
}
