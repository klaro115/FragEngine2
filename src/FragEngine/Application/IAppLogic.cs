using FragEngine.EngineCore;

namespace FragEngine.Application;

/// <summary>
/// Interface for classes that provide the main over-arching application logic.
/// Implementations of this type are responsible for controlling the engine's states, lifecycle, and spawning the app's own logic.
/// </summary>
public interface IAppLogic
{
	#region Methods

	// LIFECYCLE:

	/// <summary>
	/// Initializes the application logic, and assigns an engine.
	/// This method should only ever be called by the engine's constructor, where it is called after all systems have been assigned,
	/// and the dependency injection system is initialized.
	/// </summary>
	/// <param name="_engine">The engine instance that this app logic should be assigned to.</param>
	/// <returns>True if initialization succeeded, false otherwise.</returns>
	bool Initialize(Engine _engine);
	/// <summary>
	/// Shuts down the application logic.
	/// This method should only ever be called by the engine's exit logic, where it is called after all systems have shut down.
	/// </summary>
	void Shutdown();

	// ENGINE STATEMACHINE:

	/// <summary>
	/// Notifies the application that the engine's state is about to change.
	/// </summary>
	/// <param name="_currentState">The engine's current state, which is about to expire.</param>
	/// <param name="_targetState">The upcoming state that the engine will be transitioning to.</param>
	/// <returns>True if pre-transition logic has concluded successfully, false if a breaking error has occured.</returns>
	bool OnEngineStateChanging(EngineStateType _currentState, EngineStateType _targetState);
	/// <summary>
	/// Notifies the application that the engine's state has just changed.
	/// </summary>
	/// <param name="_previousState">The previous state, that was transitioned away from.</param>
	/// <param name="_currentState">The engine's current state, which has just become active.</param>
	/// <returns>True if post-transition logic has concluded successfully, false if a breaking error has occured.</returns>
	bool OnEngineStateChanged(EngineStateType _previousState, EngineStateType _currentState);

	// LOADING & UNLOADING:

	/// <summary>
	/// Run update logic for the engine's loading state. This is called once per frame.
	/// </summary>
	/// <param name="_hasDataScanCompleted">The loading state runs a resource data scan in the background.
	/// This indicates whether this scan has concluded; if true, all resource keys and resource manifests
	/// have been located and are ready for use. If false, assets cannot be loaded via the resource cannot
	/// services yet.</param>
	/// <param name="_outLoadingIsDone">Outputs whether application-side loading has been completed.</param>
	/// <returns>True if loading logic was updated successfully, false if a breaking error arose.</returns>
	public bool UpdateLoadingState(bool _hasDataScanCompleted, out bool _outLoadingIsDone);
	/// <summary>
	/// Run update logic for the engine's unloading state. This is called once per frame.
	/// </summary>
	/// <param name="_outUnloadingIsDone">Outputs whether application-side unloading has been completed.</param>
	/// <returns>True if loading logic was updated successfully, false if a breaking error arose.</returns>
	public bool UpdateUnloadingState(out bool _outUnloadingIsDone);

	// RUNNING:

	/// <summary>
	/// Run input processing logic for the engine's running state. This is called once at the start of each frame.
	/// </summary>
	/// <returns>True if input logic was updated successfully, false if a breaking error arose.</returns>
	public bool UpdateRunningState_Input();
	/// <summary>
	/// Run update logic for the engine's running state. This is called once per frame, after input.
	/// </summary>
	/// <returns>True if update logic ran successfully, false if a breaking error arose.</returns>
	public bool UpdateRunningState_Update();
	/// <summary>
	/// Run rendering logic for the engine's running state. This is called once towards the end of each frame, after update.
	/// </summary>
	/// <returns>True if rendering logic ran successfully, false if a breaking error arose.</returns>
	public bool UpdateRunningState_Draw();

	#endregion
}
