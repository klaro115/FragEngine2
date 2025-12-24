using FragEngine.Graphics.ConstantBuffers;
using FragEngine.Graphics.Constants;
using FragEngine.Graphics.Contexts;
using FragEngine.Graphics.Geometry;
using FragEngine.Helpers;
using FragEngine.Interfaces;
using FragEngine.Logging;
using FragEngine.Resources;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics.Renderers;

public sealed class SimpleMeshRenderer : IExtendedDisposable
{
	#region Fields

	// Services:
	private readonly GraphicsService graphicsService;
	private readonly ResourceService resourceService;
	private readonly ILogger logger;

	// Graphics resources:
	private Pipeline? pipeline = null;
	private readonly ResourceSet? resSetMesh = null;
	private readonly ResourceLayout? resLayoutMesh = null;
	private readonly DeviceBuffer? bufCbObject = null;
	private CBObject cbObject = CBObject.Default;

	// Resources:
	private MeshSurface? mesh = null;
	private ResourceHandle<MeshSurface>? meshHandle = null;
	private ResourceHandle<Shader>? vertexShaderHandle = null;
	private ResourceHandle<Shader>? pixelShaderHandle = null;

	// Checksums:
	private uint currentFrameIdx = uint.MaxValue;
	private ulong pipelineChecksum = GraphicsConstants.UNINITIALIZED_CHECKSUM;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets or sets whether to skip drawing until all resources have been loaded.
	/// If true, any calls to '<see cref="Draw(in CameraPassContext)"/>' will return true
	/// but not issue any draw calls.
	/// </summary>
	public bool DontDrawUntilFullyLoaded { get; set; } = true;

	/// <summary>
	/// Gets or sets the mesh that will be drawn by this renderer.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Mesh may not be disposed.</exception>
	public MeshSurface? Mesh
	{
		get => mesh;
		set
		{
			ObjectDisposedException.ThrowIf(value is not null && value.IsDisposed, value!);
			mesh = value;
			meshHandle = null;
		}
	}

	/// <summary>
	/// Gets or sets a handle to the the renderer's mesh.
	/// </summary>
	public ResourceHandle<MeshSurface>? MeshHandle
	{
		get => meshHandle;
		set => mesh = SetResource(value, null, ref meshHandle, false) && meshHandle.IsLoaded ? meshHandle.Resource : null;
	}

	/// <summary>
	/// Gets or sets a handle to the the renderer's vertex shader.
	/// </summary>
	public ResourceHandle<Shader>? VertexShaderHandle
	{
		get => vertexShaderHandle;
		set => SetResource(value, null, ref vertexShaderHandle, true);
	}

	/// <summary>
	/// Gets or sets a handle to the the renderer's pixel shader.
	/// </summary>
	public ResourceHandle<Shader>? PixelShaderHandle
	{
		get => pixelShaderHandle;
		set => SetResource(value, null, ref pixelShaderHandle, true);
	}

	/// <summary>
	/// Gets or sets a unique resource identifier for the renderer's mesh.
	/// </summary>
	public string MeshKey
	{
		get => meshHandle is not null ? meshHandle.ResourceKey : string.Empty;
		set => mesh = SetResource(null, value, ref meshHandle, false) && meshHandle.IsLoaded ? meshHandle.Resource : null;
	}

	/// <summary>
	/// Gets or sets a unique resource identifier for the renderer's vertex shader.
	/// </summary>
	public string VertexShaderKey
	{
		get => vertexShaderHandle is not null ? vertexShaderHandle.ResourceKey : string.Empty;
		set => SetResource(null, value, ref vertexShaderHandle, true);
	}

	/// <summary>
	/// Gets or sets a unique resource identifier for the renderer's pixel shader.
	/// </summary>
	public string PixelShaderKey
	{
		get => pixelShaderHandle is not null ? pixelShaderHandle.ResourceKey : string.Empty;
		set => SetResource(null, value, ref pixelShaderHandle, true);
	}

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new simple mesh renderer.
	/// </summary>
	/// <param name="_graphicsService">The engine`s graphics service.</param>
	/// <param name="_logger">The engine`s logging service.</param>
	/// <exception cref="ArgumentNullException">Graphics service, resource service, logger may not be null.</exception>
	/// <exception cref="ObjectDisposedException">Graphics service may not be disposed.</exception>
	/// <exception cref="Exception">Failure to create basic resources.</exception>
	public SimpleMeshRenderer(GraphicsService _graphicsService, ResourceService _resourceService, ILogger _logger)
	{
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ArgumentNullException.ThrowIfNull(_resourceService);
		ArgumentNullException.ThrowIfNull(_logger);
		ObjectDisposedException.ThrowIf(_graphicsService.IsDisposed, _graphicsService);

		graphicsService = _graphicsService;
		resourceService = _resourceService;
		logger = _logger;

		if (!CreateBasicResources(out resLayoutMesh, out bufCbObject, out resSetMesh))
		{
			throw new Exception($"Failed to create basic resources for {nameof(SimpleMeshRenderer)}!");
		}
	}

	~SimpleMeshRenderer()
	{
		if (!IsDisposed) Dispose(false);
	}

	#endregion
	#region Methods

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Dispose(true);
	}
	private void Dispose(bool _)
	{
		IsDisposed = true;

		pipeline?.Dispose();
		resSetMesh?.Dispose();
		resLayoutMesh?.Dispose();
		bufCbObject?.Dispose();
	}

	private bool CreateBasicResources(out ResourceLayout _outResLayout, out DeviceBuffer _outBufCbObject, out ResourceSet _outResSet)
	{
		_outResLayout = null!;
		_outBufCbObject = null!;
		_outResSet = null!;

		// Create resource layout:
		ResourceLayoutDescription resLayoutDesc = new(
			CBObject.ResourceLayoutElementDesc);
		//...

		try
		{
			_outResLayout = graphicsService.ResourceFactory.CreateResourceLayout(ref resLayoutDesc);
			_outResLayout.Name = $"ResLayout_{nameof(SimpleMeshRenderer)}";
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create resource layout!", ex, LogEntrySeverity.Critical);
			goto abort;
		}

		// Create object constant buffer:
		try
		{
			_outBufCbObject = graphicsService.ResourceFactory.CreateBuffer(CBObject.BufferDesc);
			_outBufCbObject.Name = $"{CBObject.resourceName}_{nameof(SimpleMeshRenderer)}";
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create constant buffer!", ex, LogEntrySeverity.Critical);
			goto abort;
		}

		// Create resource set:
		ResourceSetDescription resSetDesc = new(
			_outResLayout,
			bufCbObject);

		try
		{
			_outResSet = graphicsService.ResourceFactory.CreateResourceSet(ref resSetDesc);
			_outResSet.Name = $"ResSet_{nameof(SimpleMeshRenderer)}";
		}
		catch (Exception ex)
		{
			logger.LogException("Failed to create resource set!", ex);
			goto abort;
		}

		return true;

	abort:
		_outResLayout?.Dispose();
		_outBufCbObject?.Dispose();
		_outResSet?.Dispose();

		return false;
	}

	private bool SetResource<T>(ResourceHandle<T>? _newHandle, string? _newResourceKey, [NotNullWhen(true)] ref ResourceHandle<T>? _handle, bool _invalidatesPipeline = false)
		where T : class
	{
		if (_newHandle is null && string.IsNullOrEmpty(_newResourceKey))
		{
			logger.LogError($"Cannot assign null resource to {nameof(SimpleMeshRenderer)}!");
			return false;
		}

		// If no handle is provided, query it from resource service:
		if (_newHandle is null && !resourceService.GetResourceHandle(_newResourceKey!, out _newHandle))
		{
			logger.LogError($"Could not find resource with key '{_newResourceKey}' for {nameof(SimpleMeshRenderer)}!");
			return false;
		}

		// Queue resource up for background loading:
		if (!_newHandle.IsValid())
		{
			logger.LogError($"Cannot assign invalid resource '{_newHandle.ResourceKey}' to {nameof(SimpleMeshRenderer)}!");
			return false;
		}
		if (!_newHandle.IsLoaded && !_newHandle.Load(false))
		{
			logger.LogError($"Failed to start loading of resource '{_newHandle.ResourceKey}' for {nameof(SimpleMeshRenderer)}!");
			return false;
		}

		_handle = _newHandle;

		if (_invalidatesPipeline)
		{
			pipelineChecksum = GraphicsConstants.UNINITIALIZED_CHECKSUM;
		}
		return true;
	}

	/// <summary>
	/// Draws the renderer to the camera's framebuffer.
	/// </summary>
	/// <param name="_cameraPassCtx">A context object containing graphics infos and overarching resources for the current camera pass.</param>
	/// <returns>True if the renderer was drawn successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Camera pass context may not be null.</exception>
	public bool Draw(in CameraPassContext _cameraPassCtx)
	{
		ArgumentNullException.ThrowIfNull(_cameraPassCtx);

		if (mesh is null)
		{
			logger.LogError("Cannot draw null mesh!");
			return false;
		}
		if (DebugHelper.Check(() => IsDisposed))
		{
			logger.LogError($"Cannot draw {nameof(SimpleMeshRenderer)} that has already been disposed!");
			return false;
		}
		if (DebugHelper.Check(() => mesh.IsDisposed))
		{
			logger.LogError($"Cannot draw disposed mesh using {nameof(SimpleMeshRenderer)}!");
			return false;
		}

		// Ensure that resources are loaded:
		if (!EnsureResourcesAreLoaded(out bool allResourcesLoaded))
		{
			return false;
		}
		if (!allResourcesLoaded)
		{
			return true;
		}

		// (Re)create pipeline:
		if (pipeline is null || pipeline.IsDisposed || pipelineChecksum != _cameraPassCtx.GraphicsCtx.Checksum)
		{
			if (!BindResources(in _cameraPassCtx))
			{
				logger.LogError($"Failed to prepare resources for drawing {nameof(SimpleMeshRenderer)}!");
				return false;
			}
		}

		CommandList cmdList = _cameraPassCtx.CmdList;

		// Update constant buffer (once per frame):
		if (currentFrameIdx != _cameraPassCtx.GraphicsCtx.CbGraphics.frameIndex)
		{
			cmdList.UpdateBuffer(bufCbObject, 0, ref cbObject);
			currentFrameIdx = _cameraPassCtx.GraphicsCtx.CbGraphics.frameIndex;
		}

		// Bind pipeline:
		cmdList.SetPipeline(pipeline);

		// Bind resources:
		cmdList.SetGraphicsResourceSet(0, _cameraPassCtx.ResSetCamera);
		cmdList.SetGraphicsResourceSet(1, resSetMesh);

		// Bind geometry:
		cmdList.SetVertexBuffer(0, mesh.BufVerticesBasic);
		if (mesh.HasExtendedVertexData)
		{
			cmdList.SetVertexBuffer(1, mesh.BufVerticesExt);
		}

		cmdList.SetIndexBuffer(mesh.BufIndices, mesh.IndexFormat);

		// Issue draw call:
		cmdList.DrawIndexed((uint)mesh.IndexCount);

		return true;
	}

	private bool EnsureResourcesAreLoaded(out bool _outAllResourcesReady)
	{
		// Check if all resources are assigned:
		if (vertexShaderHandle is null || pixelShaderHandle is null || (mesh is null && meshHandle is null))
		{
			logger.LogError($"Cannot prepare resources for {nameof(SimpleMeshRenderer)}; shaders or mesh were null!");
			_outAllResourcesReady = false;
			return false;
		}

		bool shadersAreLoaded = vertexShaderHandle.IsLoaded && pixelShaderHandle.IsLoaded;
		bool meshIsLoaded = mesh is not null || meshHandle!.IsLoaded;

		// If we're at leasure to wait for async loading to complete, skip current draw call:
		if (DontDrawUntilFullyLoaded && !(shadersAreLoaded && meshIsLoaded))
		{
			_outAllResourcesReady = false;
			return true;
		}

		// Load immediately and block until complete:
		bool success = true;
		if (!vertexShaderHandle.IsLoaded)
		{
			success &= vertexShaderHandle.Load(true);
		}
		if (!pixelShaderHandle.IsLoaded)
		{
			success &= pixelShaderHandle.Load(true);
		}
		if (mesh is null && meshHandle is not null)
		{
			if (!meshHandle.IsLoaded)
			{
				success &= meshHandle.Load(true);
			}
			mesh = meshHandle.Resource;
		}

		_outAllResourcesReady = true;
		return success;
	}

	private bool BindResources(in CameraPassContext _cameraPassCtx)
	{
		pipeline?.Dispose();
		pipelineChecksum = 0;

		// Define vertex layout:
		VertexLayoutDescription[] vertexLayoutDescs = mesh!.HasExtendedVertexData
		? [
			BasicVertex.LayoutDescription,
				ExtendedVertex.LayoutDescription,
		]
		: [
			BasicVertex.LayoutDescription,
		];

		// Define shader set:
		ShaderSetDescription shaderDesc = new(
			vertexLayoutDescs,
			[
				vertexShaderHandle!.Resource,
				pixelShaderHandle!.Resource,
			]);

		// Define resource layouts:
		ResourceLayout[] resLayouts =
		[
			_cameraPassCtx.GraphicsCtx.ResLayoutCamera,
			resLayoutMesh!,
		];

		// Create pipeline:
		GraphicsPipelineDescription pipelineDesc = new(
			BlendStateDescription.SingleAdditiveBlend,
			DepthStencilStateDescription.DepthOnlyLessEqual,
			RasterizerStateDescription.CullNone,
			PrimitiveTopology.TriangleList,
			shaderDesc,
			resLayouts,
			_cameraPassCtx.OutputDescription);

		try
		{
			pipeline = graphicsService.ResourceFactory.CreateGraphicsPipeline(ref pipelineDesc);
			pipeline.Name = $"Pipeline_{nameof(SimpleMeshRenderer)}";
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create pipeline for {nameof(SimpleMeshRenderer)}!", ex);
			return false;
		}

		pipelineChecksum = _cameraPassCtx.GraphicsCtx.Checksum;
		return true;
	}

	#endregion
}
