using Veldrid;

namespace FragEngine.Extensions.Veldrid;

/// <summary>
/// Extension methods for the <see cref="Key"/> enum.
/// </summary>
public static class KeyExt
{
	#region Methods

	/// <summary>
	/// Gets whether this keyboard key maps to a valid physical key.
	/// </summary>
	/// <param name="_key"></param>
	/// <returns></returns>
	public static bool IsValid(this Key _key)
	{
		bool isValid = _key > Key.Unknown && _key < Key.LastKey;
		return isValid;
	}

	#endregion
}
