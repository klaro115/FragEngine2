using Veldrid;

namespace FragEngine.EngineCore.Input;

/// <summary>
/// Extension methods for the <see cref="InputService"/> class.
/// </summary>
/// <remarks>
/// These methods were not added directly to the service in an effort to reduce class bloating.
/// They are not necessary for normal operation and only add input definitions, or adjust settings.
/// </remarks>
public static class InputServiceExt
{
	#region Constants

	/// <summary>
	/// The standard name for the horizontal axis of WASD keys.<para/>
	/// This maps to A=negative, D=positive.
	/// </summary>
	public const string AxisNameAD = "Horizontal_WASD";
	/// <summary>
	/// The standard name for the vertical axis of WASD keys.<para/>
	/// This maps to S=negative, W=positive.
	/// </summary>
	public const string AxisNameSW = "Vertical_WASD";
	/// <summary>
	/// The standard name for the lateral/third axis of WASD+QE keys.<para/>
	/// This maps to Q=negative, E=positive.
	/// </summary>
	public const string AxisNameQE = "Lateral_WASD";

	#endregion
	#region Methods

	/// <summary>
	/// Adds input axes for WASD keyboard keys.
	/// This will add the horizontal axis A/D, and the vertical axis S/W.
	/// </summary>
	/// <param name="_service">The engine's input service singleton.</param>
	/// <returns>True if both axes were added successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Input service may not be null.</exception>
	public static bool AddInputAxesWASD(this InputService _service)
	{
		ArgumentNullException.ThrowIfNull(_service);

		bool added =
			_service.AddInputAxis(AxisNameAD, Key.A, Key.D, out _) &&
			_service.AddInputAxis(AxisNameSW, Key.S, Key.W, out _);

		if (!added)
		{
			_service.logger.LogError("Failed to add WASD input axes to input service!", Logging.LogEntrySeverity.Trivial);
		}
		return added;
	}

	/// <summary>
	/// Adds input axes for WASD+QE keyboard keys.
	/// This will add the horizontal axis A/D, the vertical axis S/W, and the lateral axis Q/E.
	/// </summary>
	/// <param name="_service">The engine's input service singleton.</param>
	/// <returns>True if all 3 axes were added successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Input service may not be null.</exception>
	public static bool AddInputAxesWASDQE(this InputService _service)
	{
		if (!AddInputAxesWASD(_service))
		{
			return false;
		}

		bool added = _service.AddInputAxis(AxisNameQE, Key.Q, Key.E, out _);
		if (!added)
		{
			_service.logger.LogError("Failed to add QE input axis to input service!", Logging.LogEntrySeverity.Trivial);
		}
		return added;
	}

	/// <summary>
	/// Adds input axes for keyboard arrow keys.
	/// This will add the horizontal axis Left/Right, and the vertical axis Down/Up.
	/// </summary>
	/// <param name="_service">The engine's input service singleton.</param>
	/// <returns>True if both axes were added successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Input service may not be null.</exception>
	public static bool AddInputAxesArrowKeys(this InputService _service)
	{
		ArgumentNullException.ThrowIfNull(_service);

		bool added =
			_service.AddInputAxis(AxisNameAD, Key.Left, Key.Right, out _) &&
			_service.AddInputAxis(AxisNameSW, Key.Down, Key.Up, out _);

		if (!added)
		{
			_service.logger.LogError("Failed to add arrow key input axes to input service!", Logging.LogEntrySeverity.Trivial);
		}
		return added;
	}

	#endregion
}
