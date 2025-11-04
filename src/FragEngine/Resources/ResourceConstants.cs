namespace FragEngine.Resources;

/// <summary>
/// Constants for resource service operation.
/// </summary>
public static class ResourceConstants
{
	#region Constants

	/// <summary>
	/// Default priority level for loading resources in the background. Lower values are loaded first.
	/// </summary>
	internal const int defaultResourceLoadPriority = 100;

	internal const int waitForLoadingToCompleteTimeoutMs = 2000;

	internal const int allResourcesStartingCapacity = 50;
	internal const int loadingQueueStartingCapacity = 50;


	public const string resourceManifestFileExtension = ".fres";


	#endregion
}
