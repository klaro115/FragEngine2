using FragEngine.Extensions;
using FragEngine.Interfaces;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.Graphics.Cameras;

/// <summary>
/// A container object wrapping a framebuffer and its underlying texture resources, for use by a <see cref="Camera"/>.
/// </summary>
public sealed class CameraTargets : IExtendedDisposable, IValidated
{
	#region Properties

	public bool IsDisposed { get; private set; } = false;

	/// <summary>
	/// Gets whether this camera target has ownership of its textures and framebuffers.
	/// If true, they will automatically be disposed when this instance expires.
	/// </summary>
	public bool HasOwnershipOfResources { get; init; } = true;

	/// <summary>
	/// The framebuffer onto which the camera will render.
	/// </summary>
	public required Framebuffer Framebuffer { get; init; }
	/// <summary>
	/// An array of textures that serve as color targets. If null or empty, this camera target only has a depth buffer.
	/// </summary>
	public required Texture[]? ColorTargets { get; init; }
	/// <summary>
	/// A texture that serves as a depth/stencil buffer. If null, this camera target only has color targets.
	/// </summary>
	public required Texture? DepthStencilBuffer { get; init; }

	/// <summary>
	/// Gets the main target texture of this camera target.
	/// If there are color targets, this will return the first color texture, otherwise it's the depth/stencil buffer.
	/// </summary>
	public Texture? MainTexture => HasColorTargets ? ColorTargets![0] : DepthStencilBuffer;

	/// <summary>
	/// Gets whether this camera target has one or more color targets.
	/// </summary>
	public bool HasColorTargets => ColorTargets is not null && ColorTargets.Length != 0;
	/// <summary>
	/// Gets whether this camera target has a depth buffer.
	/// </summary>
	public bool HasDepthBuffer => DepthStencilBuffer is not null;
	/// <summary>
	/// Gets whether this camera target has a stencil buffer.
	/// </summary>
	public bool HasStencilBuffer => HasDepthBuffer && DepthStencilBuffer!.Format.IsStencilFormat();
	/// <summary>
	/// Gets the number of samples per pixel, MSAA only.
	/// </summary>
	public TextureSampleCount SampleCount => MainTexture?.SampleCount ?? TextureSampleCount.Count1;

	public OutputDescription OutputDescription => Framebuffer.OutputDescription;

	#endregion
	#region Constructors

	~CameraTargets()
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
		
		if (HasOwnershipOfResources)
		{
			Framebuffer?.Dispose();
			
			if (ColorTargets is not null)
			{
				foreach (Texture colorTarget in ColorTargets)
				{
					colorTarget?.Dispose();
				}
			}

			DepthStencilBuffer?.Dispose();
		}
	}

	public bool IsValid()
	{
		bool isValid =
			!IsDisposed &&
			Framebuffer is not null &&
			!Framebuffer.IsDisposed &&
			(HasColorTargets || HasDepthBuffer) &&
			(!HasColorTargets || (ColorTargets![0] is not null && !ColorTargets[0].IsDisposed)) &&
			(!HasDepthBuffer || (DepthStencilBuffer is not null && !DepthStencilBuffer.IsDisposed));
		return isValid;
	}

	/// <summary>
	/// Creates a new camera target with a single color target from a camera's output settings.
	/// </summary>
	/// <param name="_graphicsService">The graphics service on whose graphics device the resources will be created.</param>
	/// <param name="_logger">A logging service for recording errors.</param>
	/// <param name="_outputSettings">A settings object describing the desired format of the camera's output.</param>
	/// <param name="_outTargets">Outputs a new camera target. Null on failure.</param>
	/// <returns>True if a new camera target was created, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Graphics service, logger, and output settings may not be null.</exception>
	public static bool Create(GraphicsService _graphicsService, ILogger _logger, CameraOutputSettings _outputSettings, out CameraTargets? _outTargets)
	{
		ArgumentNullException.ThrowIfNull(_graphicsService);
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_outputSettings);

		Texture? texColor = null;
		Texture? texDepth = null;
		Framebuffer? framebuffer = null;

		// Create color targets:
		if (_outputSettings.HasColorTarget)
		{
			TextureDescription colorDesc = new(
				_outputSettings.ResolutionX,
				_outputSettings.ResolutionY,
				1u,
				1u,
				1u,
				_outputSettings.ColorFormat,
				TextureUsage.RenderTarget | TextureUsage.Sampled,
				TextureType.Texture2D,
				_outputSettings.SampleCount);

			try
			{
				texColor = _graphicsService.ResourceFactory.CreateTexture(ref colorDesc);
				texColor.Name = $"CameraTarget_TexColor_{_outputSettings.ResolutionX}x{_outputSettings.ResolutionY}p_{_outputSettings.ColorFormat}";
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to create color target texture for camera target!", ex);
				goto abort;
			}
		}

		// Create depth/stencil target:
		if (_outputSettings.HasDepthBuffer)
		{
			TextureDescription depthDesc = new(
				_outputSettings.ResolutionX,
				_outputSettings.ResolutionY,
				1u,
				1u,
				1u,
				_outputSettings.DepthFormat,
				TextureUsage.RenderTarget | TextureUsage.Sampled | TextureUsage.DepthStencil,
				TextureType.Texture2D,
				_outputSettings.SampleCount);

			try
			{
				texDepth = _graphicsService.ResourceFactory.CreateTexture(ref depthDesc);
				texDepth.Name = $"CameraTarget_TexDepth_{_outputSettings.ResolutionX}x{_outputSettings.ResolutionY}p_{_outputSettings.DepthFormat}";
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to create depth/stencil buffer texture for camera target!", ex);
				goto abort;
			}
		}

		// Create framebuffer:
		{
			FramebufferDescription framebufferDesc = texColor is not null
				? new(texDepth, texColor)
				: new(texDepth);

			try
			{
				framebuffer = _graphicsService.ResourceFactory.CreateFramebuffer(ref framebufferDesc);
				framebuffer.Name = $"CameraTarget_Framebuffer_{_outputSettings.ResolutionX}x{_outputSettings.ResolutionY}p_Checksum={_outputSettings.Checksum:X}";
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to create depth/stencil buffer texture for camera target!", ex);
				_outTargets = null;
				return false;
			}
		}

		// Assemble camera target:
		_outTargets = new()
		{
			HasOwnershipOfResources = true,
			ColorTargets = texColor is not null ? [texColor] : null,
			DepthStencilBuffer = texDepth,
			Framebuffer = framebuffer,
		};
		if (!_outTargets.IsValid())
		{
			_logger.LogError($"Failed to create camera target from textures and framebuffer! (Output settings: {_outputSettings})");
			goto abort;
		}

		return true;

	abort:
		framebuffer?.Dispose();
		texColor?.Dispose();
		texDepth?.Dispose();
		_outTargets = null;
		return false;
	}

	#endregion
}
