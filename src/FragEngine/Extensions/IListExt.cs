namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="IList{T}"/> and <see cref="IReadOnlyList{T}"/> interfaces.
/// </summary>
public static class IListExt
{
	#region Methods

	/// <summary>
	/// Copy elements from this list to another.
	/// </summary>
	/// <typeparam name="T">The type of list elements.</typeparam>
	/// <param name="_list">This list, from where we want to copy.</param>
	/// <param name="_destination">The destination list that we want to copy to.</param>
	/// <param name="_startIndex">The index at which to start copying in the source list.</param>
	/// <param name="_count">The number of elements to copy.</param>
	/// <exception cref="ArgumentNullException">Source and destination lists may not be null.</exception>
	/// <exception cref="IndexOutOfRangeException">Start index was out of range.</exception>
	/// <exception cref="ArgumentException">Element count was invalid or out of bounds.</exception>
	public static void CopyTo<T>(this IReadOnlyList<T> _list, IList<T> _destination, int _startIndex, int _count)
	{
		ArgumentNullException.ThrowIfNull(_list);
		ArgumentNullException.ThrowIfNull(_destination);
		
		if (_startIndex < 0 || _startIndex >= _list.Count)
		{
			throw new IndexOutOfRangeException($"Copy start index {nameof(_startIndex)} is out of range!");
		}
		int endIndex = _startIndex + _count;
		if (_count < 0 || endIndex > _list.Count || _count > _destination.Count)
		{
			throw new ArgumentException("Copy count is out of range!", nameof(_count));
		}

		for (int i = 0; i < _count; i++)
		{
			int srcIndex = _startIndex + i;
			_destination[srcIndex] = _list[i];
		}
	}

	/// <summary>
	/// Try to add an element to the list, unless it already exists in the list.
	/// </summary>
	/// <typeparam name="T">The type of list elements.</typeparam>
	/// <param name="_list">This list.</param>
	/// <param name="_newElement">A new element that should be added to the list.</param>
	/// <returns>True if the element was added, false if it was already in the list.</returns>
	public static bool TryAdd<T>(this IList<T> _list, T _newElement) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_list);

		if (_list.Contains(_newElement))
		{
			return false;
		}

		_list.Add(_newElement);
		return true;
	}

	#endregion
}
