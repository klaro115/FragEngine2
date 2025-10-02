using FragEngine.Extensions.Veldrid;
using FragEngine.Interfaces;
using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace FragEngine.Graphics;

/// <summary>
/// A service for managing and creating various standard GPU resources that are commonly needed.
/// This includes a fallback texture if other textures are missing (i.e. <see cref="TexMissing"/>),
/// but also various solid-color textures for prototyping.
/// </summary>
/// <remarks>
/// OWNERSHIP: This service has full ownership of all its textures and resources! Do not dispose
/// any of its resources, even if you no longer need them; they are automatically disposed when this
/// service or the graphhics service expires.<para/>
/// TEXTURES: The missing texture is created immediately upon creation of this service. All other
/// default textures are created on-demand, when their getter property is first called. If creation
/// of any one default texture fails, then the missing texture is returned instead.<para/>
/// LIFECYCLE: The lifecycle of this service is tied to the engine's <see cref="GraphicsService"/>.
/// Is will be disposed immediately when the graphics service expires.
/// </remarks>
public sealed class GraphicsResourceService : IExtendedDisposable
{
	#region Fields

	private readonly GraphicsService graphicsService;
	private readonly ILogger logger;

	// Default textures:
	private Texture? texWhite = null;
	private Texture? texGrey = null;
	private Texture? texBlack = null;
	private Texture? texTransparent = null;

	private readonly object textureLockObj = new();

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// A placeholder texture for when another texture is missing.
	/// This texture is tiny and all its pixels are a bright magenta color, to make it visually
	/// obvious that textures are missing.
	/// </summary>
	public Texture TexMissing { get; private set; }

	/// <summary>
	/// Gets a tiny texture that is pure white in color.
	/// </summary>
	public Texture TexWhite
	{
		get
		{
			lock (textureLockObj)
			{
				if (texWhite is null && !CreateSolidColorTexture(RgbaByte.White, out texWhite))
				{
					logger.LogError("Failed to create white default texture!");
					return TexMissing;
				}
				return texWhite;
			}
		}
	}

	/// <summary>
	/// Gets a tiny texture that is grey in color.
	/// </summary>
	public Texture TexGrey
	{
		get
		{
			lock (textureLockObj)
			{
				if (texGrey is null && !CreateSolidColorTexture(RgbaByte.Grey, out texGrey))
				{
					logger.LogError("Failed to create grey default texture!");
					return TexMissing;
				}
				return texGrey;
			}
		}
	}

	/// <summary>
	/// Gets a tiny texture that is black in color.
	/// </summary>
	public Texture TexBlack
	{
		get
		{
			lock (textureLockObj)
			{
				if (texBlack is null && !CreateSolidColorTexture(RgbaByte.Black, out texBlack))
				{
					logger.LogError("Failed to create black default texture!");
					return TexMissing;
				}
				return texBlack;
			}
		}
	}

	/// <summary>
	/// Gets a tiny texture that is transparent. All color channels have the value 0.
	/// </summary>
	public Texture TexTransparent
	{
		get
		{
			lock (textureLockObj)
			{
				if (texTransparent is null && !CreateSolidColorTexture(new RgbaByte(0, 0, 0, 0), out texTransparent))
				{
					logger.LogError("Failed to create transparent default texture!");
					return TexMissing;
				}
				return texTransparent;
			}
		}
	}

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new graphics resources service.
	/// </summary>
	/// <param name="_graphicsService">The engine's graphics service singleton.</param>
	/// <param name="_logger">The engine's logging service singleton.</param>
	/// <exception cref="ArgumentNullException">Graphics service and logger may not be null.</exception>
	/// <exception cref="Exception">Failure to create placeholder resource for missing textures.</exception>
	public GraphicsResourceService(GraphicsService _graphicsService, ILogger _logger)
	{
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ArgumentNullException.ThrowIfNull(_logger);

		graphicsService = _graphicsService;
		logger = _logger;

		logger.LogStatus("# Initializing graphics resources service.");

		// Create the "missing" fallback texture immediately:
		if (!CreateSolidColorTexture(new RgbaByte(255, 0, 255, 255), out Texture? texture))
		{
			Dispose();
			throw new Exception("Failed to create placeholder resource for missing textures!");
		}
		TexMissing = texture;

		// Subscribe to lifecycle events:
		graphicsService.Disposing += OnGraphicsServiceDisposing;

		logger.LogMessage($"- Default resources ready.");
	}

	~GraphicsResourceService()
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

		// Dispose default textures:
		TexMissing?.Dispose();
		texWhite?.Dispose();
		texGrey?.Dispose();
		texBlack?.Dispose();
		texTransparent?.Dispose();
		//...
	}

	private void OnGraphicsServiceDisposing() => Dispose();

	/// <summary>
	/// Creates a tiny 2D texture where all pixels have a solid color.
	/// </summary>
	/// <param name="_fillColor">The color to fill the texture with.</param>
	/// <param name="_outTexture">Outputs the new texture, or null, if texture creation fails.
	/// The caller of this method has ownership of this new texture.</param>
	/// <returns>True if a texture was created, false otherwise.</returns>
	public bool CreateSolidColorTexture(RgbaByte _fillColor, [NotNullWhen(true)] out Texture? _outTexture)
	{
		const int width = 8;
		const int height = 8;
		const int pixelCount = width * height;
		
		TextureDescription desc = new(
			width,
			height,
			1, 1, 1,
			PixelFormat.B8_G8_R8_A8_UNorm,
			TextureUsage.Sampled,
			TextureType.Texture2D);

		// Create the texture:
		try
		{
			_outTexture = graphicsService.ResourceFactory.CreateTexture(ref desc);
			_outTexture.Name = $"TexColor_8x8p_#{_fillColor.ToHexString()}";
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create solid-color texture! (Color: {_fillColor})", ex);
			_outTexture = null;
			return false;
		}

		// Initialize all pixels to the fill color:
		Span<RgbaByte> pixelBytes = stackalloc RgbaByte[pixelCount];
		pixelBytes.Fill(_fillColor);

		try
		{
			graphicsService.Device.UpdateTexture(_outTexture, pixelBytes, 0, 0, 0, width, height, 1, 0, 0);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to create solid-color texture! (Color: {_fillColor})", ex);
			_outTexture.Dispose();
			_outTexture = null;
			return false;
		}
	}

	#endregion
}
