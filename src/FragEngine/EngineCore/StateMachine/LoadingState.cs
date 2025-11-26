using FragEngine.Application;
using FragEngine.Resources.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace FragEngine.EngineCore.StateMachine;

/// <summary>
/// An engine state that loads the initial application data.
/// </summary>
/// <remarks>
/// This state operates in two stages, both updating on a main loop. The first stage scans all resource directories and
/// embedded files for resource manifests, from which a map of all resources is created. The second stage will then call
/// upon '<see cref="IAppLogic.UpdateLoadingState(out bool)"/>' to run the application's loading logic, such as showing
/// a main loading screen. Only after both stages have completed successfully does this engine state end and transition
/// to the '<see cref="RunningState"/>'.
/// </remarks>
/// <param name="_engine">The engine.</param>
/// <param name="_appLogic">The application logic instance.</param>
internal sealed class LoadingState(Engine _engine, IAppLogic _appLogic) : MainLoopEngineState(_engine, _appLogic)
{
	#region Fields

	private bool isScanningData = false;

	private Thread? dataScanThread = null;
	private TaskCompletionSource<bool>? dataScanCompletionSource = null;

	#endregion
	#region Properties

	public override EngineStateType State => EngineStateType.Loading;

	#endregion
	#region Methods

	protected override void Dispose(bool _disposing)
	{
		if (dataScanCompletionSource is not null && !dataScanCompletionSource.Task.IsCompleted)
		{
			dataScanCompletionSource?.SetCanceled();
		}

		base.Dispose(_disposing);
	}

	/// <summary>
	/// Initializes the state and starts the resource data scan on a background thread.
	/// </summary>
	/// <returns>True if the state was initialized successfully, false otherwise.</returns>
	public override bool Initialize()
	{
		if (!base.Initialize())
		{
			return false;
		}

		dataScanCompletionSource = new();
		isScanningData = true;

		// Start a separate thread that scans for resources in the background:
		try
		{
			dataScanThread = new(RunResourceDataScanThread);
			dataScanThread.Start();
		}
		catch (Exception ex)
		{
			engine.Logger.LogException("Failed to start resource data scan thread!", ex, Logging.LogEntrySeverity.Fatal);
			dataScanCompletionSource.SetCanceled();
			return false;
		}

		return true;
	}

	public override void Shutdown()
	{
		if (dataScanCompletionSource is not null && !dataScanCompletionSource.Task.IsCompleted)
		{
			dataScanCompletionSource?.SetCanceled();
		}
		isScanningData = false;

		base.Shutdown();
	}

	protected override bool ExecuteUpdateCycle(CancellationToken _token)
	{
		if (isScanningData && !UpdateDataScanCycle())
		{
			return false;
		}

		bool success = appLogic.UpdateLoadingState(!isScanningData, out bool loadingIsComplete);

		if (loadingIsComplete)
		{
			internalCancellationSource?.Cancel();
		}
		return success;
	}

	private bool UpdateDataScanCycle()
	{
		if (dataScanCompletionSource is null)
		{
			return false;
		}
		if (!dataScanCompletionSource.Task.IsCompleted)
		{
			return true;
		}

		isScanningData = false;
		return dataScanCompletionSource.Task.Result;
	}

	private void RunResourceDataScanThread()
	{
		ResourceDataService resDataService = engine.Provider.GetRequiredService<ResourceDataService>();

		// Scan embedded files and asset directories for resource manifests:
		bool success = resDataService.ScanForAllResourceData();
		if (!success)
		{
			engine.Logger.LogError("Failed to scan for resource data!", Logging.LogEntrySeverity.Critical);
		}

		dataScanCompletionSource?.SetResult(success);
	}

	#endregion
}
