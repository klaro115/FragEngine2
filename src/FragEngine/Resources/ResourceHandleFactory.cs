using FragEngine.Graphics.Geometry;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Resources;

/// <summary>
/// Factory for hard-typed instances of <see cref="ResourceHandle{T}"/>. Generic instances of resource handles are created for each resource when
/// <see cref="ResourceData"/> is loaded from a manifest. This class allows the registration of additional generic types for handles that are specific
/// combination of <see cref="ResourceType"/> and sub-type.<para/>
/// To register custom factory methods for your resource types, call '<see cref="RegisterResourceType(ResourceType, int, FuncCreateResourceHandle, bool)"/>'
/// during engine startup. The resource system will then use these definitions for the entire run-time.
/// </summary>
/// <remarks>
/// Note: All resource handle types that are natively supported and used by the engine are registered automatically. Only override natively supported
/// types if you know exactly what you're doing.
/// </remarks>
public sealed class ResourceHandleFactory
{
	#region Types

	private readonly record struct TypeKey(ResourceType Type, int SubType)
	{
		public override string ToString() => $"{Type} ({SubType})";
	}

	#endregion
	#region Fields

	private readonly IServiceProvider serviceProvider;
	private readonly ILogger logger;

	private readonly ConcurrentDictionary<TypeKey, FuncCreateResourceHandle> handleFactoryMethods = [];

	private ResourceService? resourceService = null;

	#endregion
	#region Properties

	private ResourceService ResourceService => resourceService ??= serviceProvider.GetRequiredService<ResourceService>();

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new resource handle factory.
	/// </summary>
	/// <param name="_serviceProvider">The engine's service provider. This is used to query other services on-demand and at run-time.</param>
	/// <param name="_logger">The engine's logging service.</param>
	/// <exception cref="ArgumentNullException">Service provider and logger may not be null.</exception>
	/// <exception cref="Exception">Failure to register factory methods for natively supported types.</exception>
	public ResourceHandleFactory(IServiceProvider _serviceProvider, ILogger _logger)
	{
		ArgumentNullException.ThrowIfNull(_serviceProvider);
		ArgumentNullException.ThrowIfNull(_logger);

		serviceProvider = _serviceProvider;
		logger = _logger;

		if (!RegisterNativelySupportedTypes())
		{
			throw new Exception("Failed to register resource handle factory methods for engine's native types!");
		}
	}

	#endregion
	#region Methods

	private bool RegisterNativelySupportedTypes()
	{
		bool success = true;

		// Misc:
		success &= handleFactoryMethods.TryAdd(new(ResourceType.Unknown, 0), ThrowWhenTryingToCreateInvalidHandle);

		// Textures:
		success &= RegisterResourceType(ResourceType.Texture, (int)ResourceSubType_Texture.Texture1D, CreateGenericHandle<Texture>);
		success &= RegisterResourceType(ResourceType.Texture, (int)ResourceSubType_Texture.Texture2D, CreateGenericHandle<Texture>);
		success &= RegisterResourceType(ResourceType.Texture, (int)ResourceSubType_Texture.Texture3D, CreateGenericHandle<Texture>);
		success &= RegisterResourceType(ResourceType.Texture, (int)ResourceSubType_Texture.Cubemap, CreateGenericHandle<Texture>);
		
		// Shaders:
		success &= RegisterResourceType(ResourceType.Shader, (int)ResourceSubType_Shader.Compute, CreateGenericHandle<Shader>);
		success &= RegisterResourceType(ResourceType.Shader, (int)ResourceSubType_Shader.Vertex, CreateGenericHandle<Shader>);
		success &= RegisterResourceType(ResourceType.Shader, (int)ResourceSubType_Shader.Geometry, CreateGenericHandle<Shader>);
		success &= RegisterResourceType(ResourceType.Shader, (int)ResourceSubType_Shader.TesselationCtrl, CreateGenericHandle<Shader>);
		success &= RegisterResourceType(ResourceType.Shader, (int)ResourceSubType_Shader.TesselationEval, CreateGenericHandle<Shader>);
		success &= RegisterResourceType(ResourceType.Shader, (int)ResourceSubType_Shader.Pixel, CreateGenericHandle<Shader>);
		//...

		// 3D Models:
		success &= RegisterResourceType(ResourceType.Model, (int)ResourceSubType_Model.PolygonMesh, CreateGenericHandle<MeshSurface>);
		//...

		// Text & string-serialized formats:
		success &= RegisterResourceType(ResourceType.Text, 0, CreateGenericHandle<string>);
		success &= RegisterResourceType(ResourceType.SerializedData, (int)ResourceSubType_SerializedData.JSON, CreateGenericHandle<string>);
		success &= RegisterResourceType(ResourceType.SerializedData, (int)ResourceSubType_SerializedData.XML, CreateGenericHandle<string>);
		success &= RegisterResourceType(ResourceType.SerializedData, (int)ResourceSubType_SerializedData.CSV, CreateGenericHandle<string>);
		success &= RegisterResourceType(ResourceType.SerializedData, (int)ResourceSubType_SerializedData.YML, CreateGenericHandle<string>);
		//...

		//... (add support for more types as they are implemented)

		return success;
	}

	private bool CreateGenericHandle<T>(ResourceData _data, [NotNullWhen(true)] out ResourceHandle? _outHandle) where T : class
	{
		_outHandle = new ResourceHandle<T>(ResourceService)
		{
			ResourceKey = _data.ResourceKey,
			ResourceID = ResourceHandle.CreateResourceID(_data.ResourceKey, _data.Type),
		};
		return true;
	}

	/// <summary>
	/// This is a fallback method that exists purely to prevent the app from crashing long enough so that useful error logs may be written.
	/// This fallback is only ever used if an unknown and unregistered resource type somehow manages to slip through, or if some truly bizarre
	/// race-condition-induced madness occurs. Basically, if this method is called, you done goofed, and you should revise the way you register
	/// factory methods for your custom resources types and sub-type.
	/// </summary>
	/// <param name="_data">Resource data of a type that hasn't been registered properly.</param>
	/// <param name="_outHandle">Outputs a dysfunctional resource handle of type '<see cref="ResourceHandle{object}"/>'.</param>
	/// <returns>True, even though you don't deserve it.</returns>
	private bool CreateHandleFallback(ResourceData _data, [NotNullWhen(true)] out ResourceHandle? _outHandle)
	{
		logger.LogWarning($"Creating fallback resource handle type for resource of unknown type. (Resource data: '{_data}')");

		_outHandle = new ResourceHandle<object>(ResourceService)
		{
			ResourceKey = _data.ResourceKey,
			ResourceID = ResourceHandle.CreateResourceID(_data.ResourceKey, _data.Type),
		};
		return true;
	}

	/// <summary>
	/// If this method is ever called, you were trying to create a resource handle of a blantantly invalid type.
	/// An exception will cause your app to crash, and you should feel bad.
	/// </summary>
	/// <param name="_data">Obviously invalid resource data.</param>
	/// <param name="_outHandle">Outputs nothing.</param>
	/// <returns>Doesn't return.</returns>
	/// <exception cref="ArgumentException">Your resource data is invalid. Fix it.</exception>
	private static bool ThrowWhenTryingToCreateInvalidHandle(ResourceData _data, out ResourceHandle _outHandle)
	{
		throw new ArgumentException("Cannot create resource handle for invalid resource data!", nameof(_data));
	}

	/// <summary>
	/// Tries to register a new factory method for resource handles of a specific resource type.
	/// </summary>
	/// <param name="_resourceType">The type of resource that needs a new handle factory. May not be '<see cref="ResourceType.Unknown"/>'.</param>
	/// <param name="_resourceSubType">The sub-type index of a resource type. Default is zero. May not be negative.<para/>
	/// NOTE: Sub-type 0 will always be used as fallback if no specialized factory method is registered for non-zero sub-types.
	/// This presumes that sub-type 0 must be the default variant of any given resource type.</param>
	/// <param name="_factoryMethod">A method delegate through which a new hard-typed resource handle can be created. May not be null.</param>
	/// <param name="_overrideExistingTypes">Whether to replace an existing factory method, if another factory method has already been registered for the same
	/// resource type and sub-type.</param>
	/// <returns>True if a new factory method was registered, or if an existing method was replaced. False on error, or when trying to replace a protected type's factory.</returns>
	/// <exception cref="ArgumentNullException">Factory method delegate may not be null.</exception>
	public bool RegisterResourceType(ResourceType _resourceType, int _resourceSubType, FuncCreateResourceHandle _factoryMethod, bool _overrideExistingTypes = false)
	{
		ArgumentNullException.ThrowIfNull(_factoryMethod);

		if (_resourceType == ResourceType.Unknown)
		{
			logger.LogError("Cannot register resource handle factory method for invalid resource type!");
			return false;
		}
		if (_resourceSubType < 0)
		{
			logger.LogError("Cannot register resource handle factory method for invalid resource sub-type!");
			return false;
		}

		TypeKey key = new(_resourceType, _resourceSubType);

		// Check for collisions with existing factory methods:
		if (handleFactoryMethods.ContainsKey(key))
		{
			if (_overrideExistingTypes)
			{
				logger.LogError("Cannot register resource handle factory method for invalid resource type!");
				return false;
			}
			if (!handleFactoryMethods.TryRemove(key, out _))
			{
				logger.LogError($"Failed to replace existing resource handle factory method for resource type '{key}'!");
				return false;
			}
		}

		// Register new factory method:
		if (!handleFactoryMethods.TryAdd(key, _factoryMethod))
		{
			logger.LogError($"Failed to register resource handle factory method for resource type '{key}'!");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Tries to create a new resource handle for a given resource's data.
	/// </summary>
	/// <remarks>
	/// Warning: This does not check if another resource with the same key already exists. A handle should only ever be created once per key.
	/// Resource handles are immutable after creation.
	/// </remarks>
	/// <param name="_data">The resource data, describing the resource's type and import mechanism.</param>
	/// <param name="_outHandle">Outputs a newly created resource handle, or null, on error.</param>
	/// <returns>True if a handle was created successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Resource data may not be null.</exception>
	/// <exception cref="ArgumentException">Resource type may not be unknown, and sub-type may not be negative.</exception>
	internal bool TryCreateResourceHandle(ResourceData _data, [NotNullWhen(true)] out ResourceHandle? _outHandle)
	{
		ArgumentNullException.ThrowIfNull(_data);

		// Try to retrieve factory method for the data's type and sub-type:
		TypeKey key = new(_data.Type, _data.SubType);

		if (!handleFactoryMethods.TryGetValue(key, out FuncCreateResourceHandle? factoryMethod))
		{
			// Try again with default sub-type:
			logger.LogWarning($"No resource handle factory methods have been registered for sub-type {_data.SubType} of resource type '{_data.Type}'; trying sub-type '{key.SubType}' instead.");
			key = new(_data.Type, 0);

			if (!handleFactoryMethods.TryGetValue(key, out factoryMethod))
			{
				// No luck? Log error, and try using a typeless fallback: (this assumes that users of the resource implement their own fallback logic for faulty resources)
				logger.LogError($"No resource handle factory methods have been registered for resource type '{_data.Type}'!");
				factoryMethod = CreateHandleFallback;
			}
		}

		// Create resource handle:
		if (!factoryMethod(_data, out _outHandle))
		{
			logger.LogError($"Failed to create resource handle for resource '{_data.ResourceKey}' of type '{key}'!");
			return false;
		}

		return true;
	}

	#endregion
}
