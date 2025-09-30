using FragEngine.Resources.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.Resources;

/// <summary>
/// Extension methods for adding resource/asset-related services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class ResourcesServiceCollectionExt
{
	#region Constants

	/// <summary>
	/// The minimum number of resource services that are added by default. This constant is used as a
	/// reference to check if the service provider contains a realistic number of services for normal
	/// engine operation.
	/// </summary>
	public const int defaultServiceCount = 2;

	#endregion
	#region Methods

	public static IServiceCollection UseResources(this IServiceCollection _serviceCollection)
	{
		ArgumentNullException.ThrowIfNull(_serviceCollection);

		if (!AddResourceSources(_serviceCollection))
		{
			throw new Exception("Failed to add resource sources to service collection!");
		}
		//...

		return _serviceCollection;
	}

	private static bool AddResourceSources(IServiceCollection _serviceCollection)
	{
		_serviceCollection
			.AddSingleton<FileSource>()
			.AddSingleton<EmbeddedResourceSource>();

		return true;
	}

	#endregion
}
