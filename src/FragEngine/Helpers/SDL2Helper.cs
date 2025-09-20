using FragEngine.Logging;
using System.Text;
using Veldrid.Sdl2;

namespace FragEngine.Helpers;

/// <summary>
/// Helper class for dealing with SDL2 error codes and error messages.
/// </summary>
public static class SDL2Helper
{
	#region Methods

	/// <summary>
	/// Checks an SDL return code for errors, then logs the error.
	/// </summary>
	/// <param name="_logger">The logger service through which to output error messages.</param>
	/// <param name="_sdlErrorCode">An error code. This is the value returned by some SDL functions. 0 indicates success.</param>
	/// <param name="_logMessage">A contextual message to log together with the SDL error message. If null, the SDL error is logged by itself.</param>
	/// <param name="_clearErrorAfter">Whether to clear the error after it has been read. Should be true by default.</param>
	/// <returns>True if there was an error, false if no error was found and it's safe to proceed.</returns>
	/// <exception cref="ArgumentNullException">Logger may not be null.</exception>
	public static bool CheckAndLogError(ILogger _logger, int _sdlErrorCode, string? _logMessage, bool _clearErrorAfter = true)
	{
		ArgumentNullException.ThrowIfNull(_logger);

		if (_sdlErrorCode == 0)
		{
			return false;
		}

		GetError(out string errorMessage, _clearErrorAfter);

		LogSdlError(_logger, _logMessage, errorMessage, _sdlErrorCode);
		return true;
	}

	/// <summary>
	/// Checks for errors, then converts the message bytes to UTF-16 for output.
	/// </summary>
	/// <param name="_outErrorMessage">Outputs the error message, if an error is pending. Outputs an empty string if there is no message.</param>
	/// <param name="_clearErrorAfter">Whether to clear the error after it has been read. Should be true by default.</param>
	/// <returns>True if an error exists and the message could be parsed, false otherwise</returns>
	public static unsafe bool GetError(out string _outErrorMessage, bool _clearErrorAfter = true)
	{
		byte* pErrorBytes = Sdl2Native.SDL_GetError();
		if (pErrorBytes == null)
		{
			_outErrorMessage = string.Empty;
			return false;
		}

		const int maxMessageLength = 1024;

		int byteCount = 0;
		for (int i = 0; i < maxMessageLength; ++i)
		{
			byte c = pErrorBytes[i];
			if (c == 0)
			{
				byteCount = i;
				break;
			}
		}

		try
		{
			_outErrorMessage = Encoding.UTF8.GetString(pErrorBytes, byteCount);
		}
		catch (Exception ex)
		{
			_outErrorMessage = $"Failed to parse error message! Exception: {ex}";
			return false;
		}

		if (_clearErrorAfter)
		{
			Sdl2Native.SDL_ClearError();
		}
		return true;
	}

	/// <summary>
	/// Checks for errors, then outputs it via logger.
	/// </summary>
	/// <param name="_logger">The logger service through which to output error messages.</param>
	/// <param name="_logMessage">A contextual message to log together with the SDL error message. If null, the SDL error is logged by itself.</param>
	/// <param name="_clearErrorAfter">Whether to clear the error after it has been read. Should be true by default.</param>
	/// <returns>True if an error exists and the message could be logged, false otherwise</returns>
	/// <exception cref="ArgumentNullException">Logger may not be null.</exception>
	public static unsafe bool GetAndLogError(ILogger _logger, string? _logMessage, bool _clearErrorAfter = true)
	{
		ArgumentNullException.ThrowIfNull(_logger);

		if (!GetError(out string errorMessage, _clearErrorAfter))
		{
			return false;
		}

		LogSdlError(_logger, _logMessage, errorMessage, -1);
		return true;
	}

	private static void LogSdlError(ILogger _logger, string? _logMessage, string? _sdlErrorMessage, int _sdlErrorCode)
	{
		bool hasLogMessage = !string.IsNullOrEmpty(_logMessage);
		bool hasError = !string.IsNullOrEmpty(_sdlErrorMessage);

		if (hasError && hasLogMessage)
		{
			_logger.LogError($"{_logMessage} (SDL error: '{_sdlErrorMessage}')");
		}
		else if (hasError)
		{
			_logger.LogError($"SDL error: '{_sdlErrorMessage}'");
		}
		else if (!string.IsNullOrEmpty(_logMessage))
		{
			_logger.LogError(_logMessage);
		}
		else
		{
			_logger.LogError($"An SDL error occured! (Error code: {_sdlErrorCode})");
		}
	}

	#endregion
}
