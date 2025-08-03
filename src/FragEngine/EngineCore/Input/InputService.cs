using FragEngine.Logging;
using FragEngine.EngineCore.Windows;
using Veldrid;
using FragEngine.EngineCore.Input.Axes;
using FragEngine.EngineCore.Time;

namespace FragEngine.EngineCore.Input;

/// <summary>
/// Engine service that handles input events and axes.
/// </summary>
/// <param name="_logger">The logging service singleton.</param>
/// <param name="_timeService">The time service singleton.</param>
public sealed class InputService(ILogger _logger, TimeService _timeService)
{
	#region Fields

	private readonly ILogger logger = _logger;
	private readonly TimeService timeService = _timeService;

	private readonly Dictionary<string, InputAxis> axes = [];

	#endregion
	#region Properties

	public int AxisCount => axes.Count;

	#endregion
	#region Methods

	private void Clear()
	{
		//TODO
	}

	/// <summary>
	/// Resets and initializes all input signals for an upcoming frame.
	/// </summary>
	/// <remarks>
	/// This method should only be called by the <see cref="WindowService"/>.
	/// The snapshot will be null unless one of the application windows is focused.
	/// </remarks>
	/// <param name="snapshot">A snapshot of the input events since last frame.</param>
	/// <returns>True if inputs were updated, false on failure.</returns>
	internal bool UpdateInputSnapshot(InputSnapshot? snapshot)
	{
		Clear();

		if (snapshot is null)
		{
			return true;
		}

		float deltaTime = timeService.AppDeltaTimeSeconds;

		
		//TODO


		if (!UpdateAxes(snapshot, deltaTime))
		{
			logger.LogError("Failed to update input axes!");
			return false;
		}

		return true;
	}

	private bool UpdateAxes(InputSnapshot? snapshot, float deltaTime)
	{
		foreach (var axis in axes)
		{
			axis.Value.Update(snapshot, deltaTime);
		}

		return true;
	}

	#endregion
}
