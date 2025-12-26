namespace FragEngine.Helpers;

/// <summary>
/// Helper methods for conditional debug behaviour.
/// Most of the methods in this class will not have any effect in release builds,
/// as their code may be stripped by the compiler outside of debug builds.
/// </summary>
public static class DebugHelper
{
	#region Methods

	/// <summary>
	/// Checks if a condition is met.
	/// </summary>
	/// <remarks>
	/// The condition check will be skipped if the preprocessor macro "DEBUG" is not defined.
	/// When using this as the condition of an <see langword="if"/>-block, the code inside the
	/// conditional scope will never be executed in release builds.
	/// </remarks>
	/// <param name="_condition">The condition to check for, may not be null.</param>
	/// <returns>True if the condition is met, false otherwise.
	/// This will always return <see langword="false"/> in release builds.</returns>
	/// <exception cref="ArgumentNullException">Condition delegate may not be null.</exception>
	public static bool Check(Func<bool> _condition)
	{
#if DEBUG
		ArgumentNullException.ThrowIfNull(_condition);
		return _condition();
#else
		return false;
#endif
	}

#endregion
}
