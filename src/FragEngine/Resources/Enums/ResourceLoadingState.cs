namespace FragEngine.Resources.Enums;

/// <summary>
/// Enumeration of the different loading states a resource can be in.
/// </summary>
public enum ResourceLoadingState
{
	/// <summary>
	/// The resource has not been loaded yet.
	/// </summary>
	NotLoaded,
	/// <summary>
	/// The resource is queued up for loading, or is in the process of being loaded.
	/// </summary>
	Pending,
	/// <summary>
	/// The resource has been loaded successfully, and is ready for use.
	/// </summary>
	Loaded,
	/// <summary>
	/// Loading has failed; the resource is unavailable.
	/// </summary>
	FailedToLoad,
}
