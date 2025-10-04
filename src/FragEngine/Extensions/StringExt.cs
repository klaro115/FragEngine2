using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="string"/> type.
/// </summary>
public static class StringExt
{
	#region Types

	/// <summary>
	/// Enumeration of different text truncation behaviours.
	/// </summary>
	public enum TruncationType
	{
		Start,
		Center,
		Tail,
	}

	#endregion
	#region Methods

	/// <summary>
	/// Truncates a string to not exceed a certain length, and inserts an ellipsis ('…', unicode U+2026) where the string was cut short.
	/// </summary>
	/// <remarks>
	/// Note: This overload uses a unicode character as ellipsis, that is outside the ASCII/ANSI range. Consider using a different overload
	/// with custom ellipsis for truncated strings that will be output to console.
	/// </remarks>
	/// <param name="_txt">The original lengthy string.</param>
	/// <param name="_maxLength">The maximum number of characters before the string needs to be truncated.
	/// The truncated string with ellipsis will be no longer than this.</param>
	/// <param name="_truncationType">The truncation behaviour, i.e. where to cut an overlong string.</param>
	/// <returns>The truncated string.</returns>
	[return: NotNullIfNotNull(nameof(_txt))]
	public static string? TruncateWithEllipsis(this string? _txt, int _maxLength, TruncationType _truncationType = TruncationType.Tail)
	{
		if (_txt is null || _txt.Length < _maxLength)
		{
			return _txt;
		}

		int truncatedContentLength = _maxLength - 1;

		switch (_truncationType)
		{
			case TruncationType.Start:
				{
					return $"…{_txt.Substring(_txt.Length - truncatedContentLength)}";
				}
			case TruncationType.Center:
				{
					int truncatedPartLength = truncatedContentLength / 2;
					string startPart = _txt.Substring(truncatedPartLength);
					string tailPart = _txt.Substring(_txt.Length - truncatedPartLength);

					return $"{startPart}…{tailPart}";
				}
			case TruncationType.Tail:
				{
					return $"{_txt.Substring(truncatedContentLength)}…";
				}
			default:
				return _txt;
		}
	}

	/// <summary>
	/// Truncates a string to not exceed a certain length, and inserts an ellipsis string where the string was cut short.
	/// </summary>
	/// <param name="_txt">The original lengthy string.</param>
	/// <param name="_maxLength">The maximum number of characters before the string needs to be truncated.
	/// The truncated string with ellipsis will be no longer than this.</param>
	/// <param name="_ellipsis">A string to insert in place of an ellipsis, to indicate the string was shortened here.</param>
	/// <param name="_truncationType">The truncation behaviour, i.e. where to cut an overlong string.</param>
	/// <returns>The truncated string.</returns>
	/// <exception cref="ArgumentNullException">Ellipsis string may not be null.</exception>
	[return: NotNullIfNotNull(nameof(_txt))]
	public static string? TruncateWithEllipsis(this string? _txt, int _maxLength, string _ellipsis, TruncationType _truncationType = TruncationType.Tail)
	{
		ArgumentNullException.ThrowIfNull(_ellipsis);

		if (_txt is null || _txt.Length < _maxLength)
		{
			return _txt;
		}

		int truncatedContentLength = _maxLength - _ellipsis.Length;

		switch (_truncationType)
		{
			case TruncationType.Start:
				{
					return $"{_ellipsis}{_txt.Substring(_txt.Length - truncatedContentLength)}";
				}
			case TruncationType.Center:
				{
					int truncatedPartLength = truncatedContentLength / 2;
					string startPart = _txt.Substring(truncatedPartLength);
					string tailPart = _txt.Substring(_txt.Length - truncatedPartLength);

					return $"{startPart}{_ellipsis}{tailPart}";
				}
			case TruncationType.Tail:
				{
					return $"{_txt.Substring(truncatedContentLength)}{_ellipsis}";
				}
			default:
				return _txt;
		}
	}

	#endregion
}
