using FragEngine.EngineCore.Input.Axes;

namespace FragEngine.EngineCore.Input;

/// <summary>
/// Event listener method for when a new input axis is created.
/// </summary>
/// <param name="_newAxis">The new input axis.</param>
public delegate void FuncInputAxisAdded(InputAxis _newAxis);

/// <summary>
/// Event listener method for when an input axis is removed from <see cref="InputService"/>.
/// </summary>
/// <param name="_newAxis">The removed input axis.</param>
public delegate void FuncInputAxisRemoved(InputAxis _newAxis);
