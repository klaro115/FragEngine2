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
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics;

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

	#endregion
	#region Constructors

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

	public bool IsResourceFormatKeySupported(string _formatKey, ResourceOperationType _operation)
	{
		List<string> supportedKeysForOperation = _operation == ResourceOperationType.Import
			? supportedImportFormatkeys
			: supportedExportFormatkeys;

		return supportedKeysForOperation.Contains(_formatKey);
	}

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

		Stream stream = null!;	//TODO [Critical]: Implement logic to get streams from resource sources!

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

		Stream stream = null!;  //TODO [Critical]: Implement logic to get streams from resource sources!

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
		return false;
	}

	#endregion
}
