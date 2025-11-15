using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FragEngine.Helpers;

/// <summary>
/// Helper class for working with enums.
/// </summary>
public static class EnumHelper
{
	#region Methods

	/// <summary>
	/// Isolates all individual bit flags in an enum value that are raised and stores them in a list.
	/// </summary>
	/// <typeparam name="T">The type of the enum, must be decorated with <see cref="FlagsAttribute"/>.</typeparam>
	/// <param name="_flags">The enum value which may have any number of flags raised.</param>
	/// <param name="_discardZeroValue">Whether to ignore and skip any bit flag declarations whose numeric value is zero.</param>
	/// <returns>A list containing all raised flags in the enum value.</returns>
	public static List<T> GetRaisedFlags<T>(T _flags, bool _discardZeroValue = true) where T : unmanaged, Enum
	{
		Debug.Assert(typeof(T).GetCustomAttribute<FlagsAttribute>() is not null, $"Enum type does not have the {nameof(FlagsAttribute)} attribute.");

		List<T> raisedFlags = [];

		T[] allFlags = (T[])Enum.GetValuesAsUnderlyingType<T>();
		
		for (int i = 0; i < allFlags.Length; ++i)
		{
			if (_discardZeroValue && Unsafe.As<T, int>(ref allFlags[i]) == 0)
			{
				continue;
			}
			if (_flags.HasFlag(allFlags[i]))
			{
				raisedFlags.Add(allFlags[i]);
			}
		}

		return raisedFlags;
	}

	#endregion
}
