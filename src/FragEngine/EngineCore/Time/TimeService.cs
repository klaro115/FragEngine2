using FragEngine.Graphics;
using FragEngine.Interfaces;
using FragEngine.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace FragEngine.EngineCore.Time;

/// <summary>
/// Engine service that is responsible for tracking time.
/// </summary>
/// <param name="_logger">The logging service singleton.</param>
public sealed class TimeService : IExtendedDisposable
{
	#region Fields

	private readonly ILogger logger;
	private readonly IServiceProvider serviceProvider;

	private readonly SemaphoreSlim semaphore = new(1, 1);

	private readonly Stopwatch applicationTimeStopwatch = new();
	private readonly Stopwatch ingameTimeStopwatch = new();

	private bool isInitialized = false;

	private TimeSpan levelStartIngameTimestamp = TimeSpan.Zero;

	private TimeSpan targetDeltaTime = TimeSpan.FromMilliseconds(1000.0 / 60.0);
	private float targetFrameRate = 60;

	private readonly TimeSpan[] frameTimeBuffer = new TimeSpan[10];
	private int frameTimeBufferIdx = 0;

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; } = false;
	/// <summary>
	/// Gets whether the application is currently paused.
	/// While paused, <see cref="IngameTime"/> does not increase.
	/// </summary>
	public bool IsPaused { get; private set; } = false;

	/// <summary>
	/// Gets the date and time when the application started running, in UTC.
	/// </summary>
	public DateTime AppStartDateTimeUtc { get; } = DateTime.UtcNow;
	/// <summary>
	/// Gets the date and time of when the application was last paused, in UTC.
	/// </summary>
	public DateTime PauseStartDateTimeUtc { get; private set; } = DateTime.UtcNow;
	/// <summary>
	/// Gets the date and time of when the application was last unpaused, in UTC.
	/// </summary>
	public DateTime PauseEndDateTimeUtc { get; private set; } = DateTime.UtcNow;
	/// <summary>
	/// Gets the date and time of when the current level started, in UTC.
	/// </summary>
	public DateTime LevelStartDateTimeUtc { get; private set; } = DateTime.UtcNow;

	/// <summary>
	/// Gets the total time since the application started running.
	/// </summary>
	public TimeSpan AppTime => applicationTimeStopwatch.Elapsed;
	/// <summary>
	/// Gets the total time that the application has spent in an unpaused state.
	/// </summary>
	public TimeSpan IngameTime => ingameTimeStopwatch.Elapsed;
	/// <summary>
	/// Gets the total ingame time since the current level started.
	/// </summary>
	public TimeSpan LevelTime => ingameTimeStopwatch.Elapsed - levelStartIngameTimestamp;

	/// <summary>
	/// Gets the duration of the last frame.
	/// </summary>
	public TimeSpan AppDeltaTime { get; private set; } = TimeSpan.Zero;
	/// <summary>
	/// Gets the duration of the last frame, in seconds.
	/// </summary>
	public float AppDeltaTimeSeconds { get; private set; } = 0;

	/// <summary>
	/// Gets the duration of the last ingame frame. If paused, this will be zero.
	/// </summary>
	public TimeSpan IngameDeltaTime { get; private set; } = TimeSpan.Zero;
	/// <summary>
	/// Gets the duration of the last ingame frame, in seconds. If paused, this will be zero.
	/// </summary>
	public float IngameDeltaTimeSeconds { get; private set; } = 0;

	/// <summary>
	/// Timestamp of when the current frame started, in application time.
	/// </summary>
	public TimeSpan CurrentFrameStartTime { get; private set; }

	/// <summary>
	/// Gets the index of the current frame.<para/>
	/// This refers to frames in the context of engine update cycles.
	/// </summary>
	public uint CurrentFrameIndex { get; private set; } = 0;

	/// <summary>
	/// Gets the current frame rate average for the last 10 frames, in Hertz.
	/// </summary>
	public float CurrentFrameRate { get; private set; } = 60.0f;

	/// <summary>
	/// Gets or sets the desired delta time, i.e. the targeted minimum duration of one frame.
	/// </summary>
	public TimeSpan TargetDeltaTime
	{
		get => targetDeltaTime;
		set
		{
			semaphore.Wait();
			targetDeltaTime = value >= TimeSpan.FromMilliseconds(1) ? value : TimeSpan.FromMilliseconds(1);
			targetFrameRate = (float)(1.0 / targetDeltaTime.TotalSeconds);
			semaphore.Release();
		}
	}
	/// <summary>
	/// Gets or sets the desired frame rate, i.e. the targeted maximum number of frames per second.
	/// </summary>
	public float TargetFrameRate
	{
		get => targetFrameRate;
		set
		{
			semaphore.Wait();
			targetFrameRate = Math.Clamp(value, 1, 1000);
			targetDeltaTime = TimeSpan.FromMilliseconds(1000.0 / value);
			semaphore.Release();
		}
	}

	#endregion
	#region Constructors

	/// <summary>
	/// Creates a new time service.
	/// </summary>
	/// <param name="_logger">The engine's logging service.</param>
	/// <param name="_serviceProvider">The engine's service provider.</param>
	/// <exception cref="ArgumentNullException">Logger and service provider may not be null!</exception>
	public TimeService(ILogger _logger, IServiceProvider _serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(_logger);
		ArgumentNullException.ThrowIfNull(_serviceProvider);

		logger = _logger;
		serviceProvider = _serviceProvider;

		AppStartDateTimeUtc = DateTime.UtcNow;
		PauseStartDateTimeUtc = DateTime.UtcNow;
		PauseEndDateTimeUtc = DateTime.UtcNow;

		ingameTimeStopwatch.Start();
		applicationTimeStopwatch.Start();

		for (int i = 0; i < frameTimeBuffer.Length; ++i)
		{
			frameTimeBuffer[i] = TargetDeltaTime;
		}

		ResetLevelTime();
	}

	~TimeService()
	{
		Dispose();
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

		ingameTimeStopwatch.Stop();
		applicationTimeStopwatch.Stop();
		semaphore.Dispose();
	}

	private void Initialize()
	{
		if (IsDisposed || isInitialized) return;

		isInitialized = true;

		GraphicsService graphicsService = serviceProvider.GetRequiredService<GraphicsService>();
		graphicsService.GraphicsSettingsChanged += OnGraphicsSettingsChanged;
	}

	/// <summary>
	/// Resets the timestamp for when the current level started. Call this when a new level/stage is loaded,
	/// </summary>
	public void ResetLevelTime()
	{
		semaphore.Wait();
		levelStartIngameTimestamp = ingameTimeStopwatch.Elapsed;
		semaphore.Release();
	}

	/// <summary>
	/// Pauses ingame time.
	/// </summary>
	/// <returns>True if successfully paused, false on error or if already paused.</returns>
	public bool PauseGame()
	{
		if (IsDisposed)
		{
			logger.LogWarning("Cannot pause ingame time on time service that has already been disposed.");
			return false;
		}
		if (IsPaused)
		{
			logger.LogWarning("Ingame time is already paused.");
			return false;
		}

		semaphore.Wait();

		IsPaused = true;

		PauseStartDateTimeUtc = DateTime.UtcNow;
		PauseEndDateTimeUtc = PauseStartDateTimeUtc;
		ingameTimeStopwatch.Stop();

		semaphore.Release();
		return true;
	}

	/// <summary>
	/// Resumes ingame time.
	/// </summary>
	/// <returns>True if successfully unpaused, false on error or if already unpaused.</returns>
	public bool Unpause()
	{
		if (IsDisposed)
		{
			logger.LogWarning("Cannot unpause ingame time on time service that has already been disposed.");
			return false;
		}
		if (!IsPaused)
		{
			logger.LogWarning("Ingame time is already unpaused.");
			return false;
		}

		semaphore.Wait();

		IsPaused = false;

		PauseEndDateTimeUtc = DateTime.UtcNow;
		ingameTimeStopwatch.Start();

		semaphore.Release();
		return true;
	}

	/// <summary>
	/// Begins a new frame.
	/// </summary>
	/// <returns>True on success, false otherwise.</returns>
	internal bool BeginFrame()
	{
		if (IsDisposed)
		{
			logger.LogError("Time service has already been disposed!", LogEntrySeverity.High);
			return false;
		}
		if (!isInitialized)
		{
			Initialize();
		}

		semaphore.Wait();

		CurrentFrameStartTime = applicationTimeStopwatch.Elapsed;
		
		semaphore.Release();
		return true;
	}

	/// <summary>
	/// Ends the current frame, and updates timers and delta times.
	/// </summary>
	/// <param name="_outFrameSleepTime">Outputs </param>
	/// <returns></returns>
	internal bool EndFrame(out TimeSpan _outFrameSleepTime)
	{
		if (IsDisposed)
		{
			logger.LogError("Time service has already been disposed!", LogEntrySeverity.High);
			_outFrameSleepTime = TimeSpan.Zero;
			return false;
		}

		semaphore.Wait();

		// Increment global frame index:
		CurrentFrameIndex++;

		// Update app delta times:
		TimeSpan currentFrameEndTime = applicationTimeStopwatch.Elapsed;

		AppDeltaTime = currentFrameEndTime - CurrentFrameStartTime;
		AppDeltaTimeSeconds = (float)AppDeltaTime.TotalSeconds;

		// Update ingame delta times:
		if (IsPaused)
		{
			IngameDeltaTime = TimeSpan.Zero;
			IngameDeltaTimeSeconds = 0;
		}
		else
		{
			IngameDeltaTime = AppDeltaTime;
			IngameDeltaTimeSeconds = AppDeltaTimeSeconds;
		}

		// Calculate smoothed frame rate:
		frameTimeBuffer[frameTimeBufferIdx++] = AppDeltaTime;
		if (frameTimeBufferIdx >= frameTimeBuffer.Length)
		{
			frameTimeBufferIdx = 0;
		}
		TimeSpan averageFrameTime = TimeSpan.Zero;
		foreach (TimeSpan frameTime in frameTimeBuffer)
		{
			averageFrameTime += frameTime;
		}
		averageFrameTime /= frameTimeBuffer.Length;
		CurrentFrameRate = 1.0f / (float)averageFrameTime.TotalSeconds;

		// Calculate thread sleep time:
		if (AppDeltaTime < TargetDeltaTime)
		{
			_outFrameSleepTime = TargetDeltaTime - AppDeltaTime;
		}
		else
		{
			_outFrameSleepTime = TimeSpan.Zero;
		}

		semaphore.Release();
		return true;
	}

	private void OnGraphicsSettingsChanged(GraphicsSettings? _previousSettings, GraphicsSettings _currentSettings)
	{
		if (IsDisposed) return;

		TargetFrameRate = _currentSettings.FrameRateLimit;
	}

	#endregion
}
