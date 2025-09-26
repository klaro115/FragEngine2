using System.Diagnostics.CodeAnalysis;

namespace FragEngine.Resources.Sources;

/// <summary>
/// A resource source that loads resource data from an assembly's embedded resources.
/// </summary>
public sealed class EmbeddedResourceSource : IResourceSource
{
	#region Properties

	public bool IsDisposed => false;

	#endregion
	#region Methods

	public void Dispose() { }

	public bool IsValid()
	{
		throw new NotImplementedException();
	}

	public bool CheckIfResourceExists(string? _sourceKey, int _sourceId)
	{
		throw new NotImplementedException();
	}

	public bool OpenResourceStream(string? _sourceKey, int _sourceId, [NotNullWhen(true)] out Stream? _outStream)
	{
		throw new NotImplementedException();
	}

	#endregion
}
