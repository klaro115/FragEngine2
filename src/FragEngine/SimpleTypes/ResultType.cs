namespace FragEngine.SimpleTypes;

/// <summary>
/// Enumeration of different possible outcome types for an action.
/// </summary>
public enum ResultType
{
	/// <summary>
	/// Task was completed successfully.
	/// </summary>
	Success,
	/// <summary>
	/// Task resulted in failure, or was aborted.
	/// </summary>
	Failure,
	/// <summary>
	/// Task outcome was neutral, undefined, or resulted in no changes.
	/// </summary>
	None,
}
