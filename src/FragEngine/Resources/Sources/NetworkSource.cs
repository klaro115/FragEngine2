using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Sources;

/// <summary>
/// A resource source that loads resource data from the network using a URL or an IP address.
/// </summary>
public sealed class NetworkSource : IResourceSource
{
	#region Fields

	//...

	#endregion
	#region Properties

	public bool IsDisposed { get; private set; }

	#endregion
	#region Properties

	~NetworkSource()
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
		//...
	}

	public bool IsValid()
	{
		return false;
	}

	public bool CheckIfResourceExists(string? _sourceKey, int _sourceId)
	{
		throw new NotImplementedException("Network source logic has not been implemented yetv.");
	}

	public bool OpenResourceStream(string? _sourceKey, int _sourceId, [NotNullWhen(true)] out Stream? _outStream)
	{
		throw new NotImplementedException("Network source logic has not been implemented yet.");
	}

	#endregion
}
