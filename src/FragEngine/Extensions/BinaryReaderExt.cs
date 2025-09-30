namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="BinaryReader"/> class.
/// </summary>
public static class BinaryReaderExt
{
	#region Methods

	/// <summary>
	/// Moves the position of the reader's underlying stream to a specific position.
	/// </summary>
	/// <param name="_reader">This binary reader. May not be null or disposed.</param>
	/// <param name="_targetPosition">The target position that we want to jump to.</param>
	/// <exception cref="ArgumentNullException">Binary reader may not be null.</exception>
	/// <exception cref="ArgumentException">Target position may not be negative.</exception>
	/// <exception cref="InvalidOperationException">Underlying stream cannot seek, but target lies before read position.</exception>
	public static void JumpTo(this BinaryReader _reader, long _targetPosition)
	{
		ArgumentNullException.ThrowIfNull(_reader);

		if (_targetPosition < 0)
		{
			throw new ArgumentException("Binary reader cannot jump to negative target position!", nameof(_targetPosition));
		}
		if (_reader.BaseStream.Position == _targetPosition)
		{
			return;
		}

		if (_reader.BaseStream.CanSeek)
		{
			_reader.BaseStream.Position = _targetPosition;
			return;
		}

		long targetOffset = _targetPosition - _reader.BaseStream.Position;
		if (targetOffset < 0)
		{
			throw new InvalidOperationException("Binary reader cannot seek; unable to jump to target position before current read position!");
		}

		for (int i = 0; i < targetOffset; ++i)
		{
			_reader.ReadByte();
		}
	}

	#endregion
}
