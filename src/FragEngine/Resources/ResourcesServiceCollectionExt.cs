using FragEngine.Extensions;
using FragEngine.Resources.Serialization;
using FragEngine.Resources.Sources;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

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

		_serviceCollection
			.AddSingleton<ResourceService>();
			//...

		if (!AddSerialization(_serviceCollection))
		{
			throw new Exception("Failed to add serialization servies to service collection!");
		}

		if (!AddResourceSources(_serviceCollection))
		{
			throw new Exception("Failed to add resource sources to service collection!");
		}
		//...

		return _serviceCollection;
	}

	private static bool AddSerialization(IServiceCollection _serviceCollection)
	{
		// Add a set of default options for JSON serialization:
		if (!_serviceCollection.HasService<JsonSerializerOptions>())
		{
			JsonSerializerOptions jsonOptions = new()
			{
				AllowTrailingCommas = true,
				WriteIndented = true,
				IndentCharacter = '\t',
				IgnoreReadOnlyFields = true,
				IgnoreReadOnlyProperties = true,
			};
			_serviceCollection.AddSingleton(jsonOptions);
		}

		// Add services:
		_serviceCollection
			.AddSingleton<SerializerService>();
			//...

		return true;
	}

	private static bool AddResourceSources(IServiceCollection _serviceCollection)
	{
		_serviceCollection
			.AddSingleton<FileSource>()
			.AddSingleton<EmbeddedResourceSource>();
			//...

		return true;
	}

	#endregion
}
