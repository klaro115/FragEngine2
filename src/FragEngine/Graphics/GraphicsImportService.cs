using FragEngine.Extensions.Veldrid;
using FragEngine.Graphics.Geometry;
using FragEngine.Graphics.Geometry.Export;
using FragEngine.Graphics.Geometry.Import;
using FragEngine.Graphics.Geometry.Import.FMDL;
using FragEngine.Graphics.Shaders.Export;
using FragEngine.Graphics.Shaders.Import;
using FragEngine.Logging;
using FragEngine.Resources.Data;
using FragEngine.Resources.Enums;
using FragEngine.Resources.Interfaces;
using FragEngine.Resources.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// Engine service for importing and exporting graphics resources.
/// This service maintains a map of supported file formats and their associated compatible importers.
/// </summary>
public sealed class GraphicsImportService : IImportService
{
	#region Types

	private readonly record struct TypeKey(ResourceType Type, int SubType, string FormatKey);

	#endregion
	#region Fields

	// 3D Models:
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger logger;
	private readonly FMdlImporter fmdlImporter;
	private readonly FMdlExporter fmdlExporter;
	//...

	private GraphicsService? graphicsService = null;
	private ResourceDataService? resourceDataService = null;

	// Shaders:
	private readonly SourceCodeShaderImporter sourceCodeShaderImporter;
	//...

	private readonly Dictionary<string, IModelImporter> modelImporters;
	private readonly Dictionary<string, IModelExporter> modelExporters;
	private readonly Dictionary<string, IShaderImporter> shaderImporters;
	private readonly Dictionary<string, IShaderExporter> shaderExporters;

	private readonly List<string> supportedImportFormatkeys;
	private readonly List<string> supportedExportFormatkeys;

	#endregion
	#region Properties

	public bool HasImporters => true;

	public bool HasExporters => true;

	private GraphicsService GraphicsService => graphicsService ??= serviceProvider.GetRequiredService<GraphicsService>();
	private ResourceDataService ResourceDataService => resourceDataService ??= serviceProvider.GetRequiredService<ResourceDataService>();

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new instance of the graphics resource import service.
	/// </summary>
	/// <param name="_serviceProvider">The engine's main service provider, used for querying further services at run-time.</param>
	/// <param name="_logger">A logging service, for outputting error reports.</param>
	/// <param name="_fmdlImporter">A 3D model importer for the engine's native FMDL file format.</param>
	/// <param name="_fmdlExporter">A 3D model exporter for the engine's native FMDL file format.</param>
	/// <param name="_sourceCodeShaderImporter">A shader importer for uncompiled shader source code.</param>
	/// <exception cref="ArgumentNullException">Engine services, importers, and exporters may not be null.</exception>
	public GraphicsImportService(
		IServiceProvider _serviceProvider,
		ILogger _logger,
		FMdlImporter _fmdlImporter,
		FMdlExporter _fmdlExporter,
		SourceCodeShaderImporter _sourceCodeShaderImporter)
	{
		// Set dependencies:
		ArgumentNullException.ThrowIfNull(_serviceProvider);
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_fmdlImporter);
		ArgumentNullException.ThrowIfNull(_fmdlExporter);
		ArgumentNullException.ThrowIfNull(_sourceCodeShaderImporter);

		serviceProvider = _serviceProvider;
		logger = _logger;
		fmdlImporter = _fmdlImporter;
		fmdlExporter = _fmdlExporter;
		sourceCodeShaderImporter = _sourceCodeShaderImporter;
		//...

		// Importer dictionaries:
		modelImporters = new()
		{
			[FMdlConstants.fmdlFormatKey] = fmdlImporter,
			//...
		};
		modelExporters = new()
		{
			[FMdlConstants.fmdlFormatKey] = fmdlExporter,
			//...
		};

		shaderImporters = [];
		shaderExporters = [];
		foreach (string formatKey in SourceCodeShaderImporter.supportedFormatKeys)
		{
			shaderImporters.Add(formatKey, sourceCodeShaderImporter);
		}

		// Supported format keys:
		supportedImportFormatkeys =
		[
			FMdlConstants.fmdlFormatKey,
			".dxbc",
			".dxil",
			//...
		];
		supportedImportFormatkeys.AddRange(SourceCodeShaderImporter.supportedFormatKeys);

		supportedExportFormatkeys =
		[
			FMdlConstants.fmdlFormatKey,
			//...
		];
	}

	#endregion
	#region Methods

	/// <summary>
	/// Checks the level support for a given resource type and sub-type.
	/// </summary>
	/// <param name="_type">The resource type.</param>
	/// <param name="_subType">The ID of a sub-type of the resource type '<paramref name="_type"/>'.</param>
	/// <returns>An enum indicating the level of support.
	/// Returns '<see cref="ResourceTypeSupport.None"/>' if the resource is not supported at all.</returns>
	public ResourceTypeSupport IsResourceTypeSupported(ResourceType _type, int _subType = 0)
	{
		return _type switch
		{
			ResourceType.Texture => ResourceTypeSupport.SubTypeSupported,
			ResourceType.Model => _subType == (int)ResourceSubType_Model.PolygonMesh
								? ResourceTypeSupport.SubTypeSupported
								: ResourceTypeSupport.None,
			ResourceType.Shader => ResourceTypeSupport.SubTypeSupported,
			_ => ResourceTypeSupport.None,
		};
	}

	/// <summary>
	/// Checks whether 
	/// </summary>
	/// <param name="_formatKey">The resource file's format key, typically a file extension. Must be in lowercase.</param>
	/// <param name="_operation">The type of operation that is required; basically, whether to import or export data.</param>
	/// <returns>True if the requested format is supported for the requested operation, false otherwise.</returns>
	public bool IsResourceFormatKeySupported(string _formatKey, ResourceOperationType _operation)
	{
		List<string> supportedKeysForOperation = _operation == ResourceOperationType.Import
			? supportedImportFormatkeys
			: supportedExportFormatkeys;

		return supportedKeysForOperation.Contains(_formatKey);
	}

	/// <summary>
	/// Tries to import a graphics resource from serialized data.
	/// </summary>
	/// <param name="_resourceData">Resource data describing the format and location of a resource. May not be null.</param>
	/// <param name="_outResourceInstance">Outputs the fully loaded graphics resource instance, or null, if the format
	/// is not supported or if import failed.</param>
	/// <returns>True if the resource could be imported successfully, false otherwise.</returns>
	public bool ImportResourceData(in ResourceData _resourceData, [NotNullWhen(true)] out object? _outResourceInstance)
	{
		ArgumentNullException.ThrowIfNull(_resourceData);

		_outResourceInstance = null;

		var success = _resourceData.Type switch
		{
			ResourceType.Texture => ImportTexture(in _resourceData, out _outResourceInstance),
			ResourceType.Model => ImportModel(in _resourceData, out _outResourceInstance),
			ResourceType.Shader => ImportShader(in _resourceData, out _outResourceInstance),
			_ => false,
		};

		if (!success)
		{
			logger.LogError($"{nameof(GraphicsImportService)} failed to import resource '{_resourceData}'!");
		}
		return success;
	}

	//... (add export later)

	#endregion
	#region Methods Import

	private bool ImportTexture(in ResourceData _resourceData, [NotNullWhen(true)] out object? _outResourceInstance)
	{
		//TODO [later]: Add basic texture importers.
		_outResourceInstance = null;    //TEMP
		return false;
	}

	private bool ImportModel(in ResourceData _resourceData, [NotNullWhen(true)] out object? _outResourceInstance)
	{
		if (!modelImporters.TryGetValue(_resourceData.FormatKey, out IModelImporter? importer))
		{
			logger.LogError($"{nameof(GraphicsImportService)} could not find a model importer for resource '{_resourceData}'!");
			_outResourceInstance = null;
			return false;
		}

		if (!ResourceDataService.OpenResourceStream(in _resourceData, out Stream? stream))
		{
			logger.LogError($"{nameof(GraphicsImportService)} could not open stream for resource '{_resourceData}'!");
			_outResourceInstance = null;
			return false;
		}

		// Load surface data:
		if (!importer.LoadMeshSurfaceData(stream, out MeshSurfaceData? surfaceData))
		{
			_outResourceInstance = null;
			return false;
		}

		// Create and populate mesh:
		MeshSurface mesh = new(GraphicsService, logger);

		if (!mesh.SetData(surfaceData))
		{
			mesh.Dispose();
			_outResourceInstance = null;
			return false;
		}

		_outResourceInstance = mesh;
		return false;
	}

	private bool ImportShader(in ResourceData _resourceData, [NotNullWhen(true)] out object? _outResourceInstance)
	{
		if (!shaderImporters.TryGetValue(_resourceData.FormatKey, out IShaderImporter? importer))
		{
			logger.LogError($"{nameof(GraphicsImportService)} could not find a model importer for resource '{_resourceData}'!");
			_outResourceInstance = null;
			return false;
		}

		if (!ResourceDataService.OpenResourceStream(in _resourceData, out Stream? stream))
		{
			logger.LogError($"{nameof(GraphicsImportService)} could not open stream for resource '{_resourceData}'!");
			_outResourceInstance = null;
			return false;
		}

		// Determine import parameters:
		ShaderStages stage = _resourceData.Type.GetShaderStageForType(_resourceData.SubType);
		string entryPoint = stage.GetDefaultEntryPoint();

		// Import, compile, and uploaad shader program:
		if (!importer.LoadShaderProgram(stream, (int)_resourceData.DataSize, stage, entryPoint, out Shader? shader))
		{
			_outResourceInstance = null;
			return false;
		}

		_outResourceInstance = shader;
		return true;
	}

	#endregion
}
