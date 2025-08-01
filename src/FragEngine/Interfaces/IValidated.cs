namespace FragEngine.Interfaces;

/// <summary>
/// Interface for objects or data whose value and state can be validated.
/// </summary>
public interface IValidated
{
	#region Methods

	/// <summary>
	/// Checks the validity of values.
	/// </summary>
	/// <returns>True if values make sense, false if they're bogus.</returns>
	bool IsValid();

	#endregion
}
