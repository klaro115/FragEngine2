namespace FragEngine.Interfaces;

/// <summary>
/// Interface that extends the <see cref="IDisposable"/> interface by adding an '<see cref="IsDisposed"/>' status flag.
/// </summary>
public interface IExtendedDisposable : IDisposable
{
	#region Properties

	/// <summary>
	/// Gets whether this object has been disposed already.
	/// </summary>
	bool IsDisposed { get; }

	#endregion
}
