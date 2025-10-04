namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="BinaryWriter"/> class.
/// </summary>
public static class BinaryWriterExt
{
	#region Methods

	/// <summary>
	/// Moves the position of the writer's underlying stream to a specific position.
	/// </summary>
	/// <param name="_writer">This binary writer. May not be null or disposed.</param>
	/// <param name="_targetPosition">The target position that we want to jump to.</param>
	/// <param name="_padWithZeroIfCantSeek">Whether to write zero bytes until reaching the target position if the underlying
	/// stream doesn't support seeking. If false, an exception will be thrown instead if seeking is not supported.</param>
	/// <exception cref="ArgumentNullException">Binary writer may not be null.</exception>
	/// <exception cref="ArgumentException">Target position may not be negative.</exception>
	/// <exception cref="InvalidOperationException">Underlying stream cannot seek, but target lies before write position.</exception>
	public static void JumpTo(this BinaryWriter _writer, long _targetPosition, bool _padWithZeroIfCantSeek)
	{
		ArgumentNullException.ThrowIfNull(_writer);

		if (_targetPosition < 0)
		{
			throw new ArgumentException("Binary writer cannot jump to negative target position!", nameof(_targetPosition));
		}
		if (_writer.BaseStream.Position == _targetPosition)
		{
			return;
		}

		if (_writer.BaseStream.CanSeek)
		{
			_writer.BaseStream.Position = _targetPosition;
			return;
		}

		long targetOffset = _targetPosition - _writer.BaseStream.Position;
		if (targetOffset < 0)
		{
			throw new InvalidOperationException("Binary writer cannot seek; unable to jump to target position before current write position!");
		}
		if (!_padWithZeroIfCantSeek)
		{
			throw new InvalidOperationException("Binary writer cannot seek; unable to jump to target position ahead of current write position!");
		}

		for (int i = 0; i < targetOffset; ++i)
		{
			_writer.Write((byte)0);
		}
	}

	#endregion
}
