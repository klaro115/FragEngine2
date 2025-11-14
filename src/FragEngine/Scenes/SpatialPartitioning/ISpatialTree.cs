namespace FragEngine.Scenes.SpatialPartitioning;

/// <summary>
/// Base interface for spatial partitioning trees and collections of physical scene objects that allow location-base object retrieval.
/// </summary>
/// <typeparam name="T">The type of physical objects in this spatial tree.</typeparam>
public interface ISpatialTree<T> : IPhysicalObject where T : IPhysicalObject
{
	#region Properties

	/// <summary>
	/// Gets the total number of objects that currently in this spatial tree.
	/// </summary>
	public int ObjectCount { get; }
	/// <summary>
	/// Gets the maximum hierarchical/recursive depth of this spatial tree.
	/// </summary>
	/// <remarks>
	/// A depth of 1 means that the tree has only one level, a depth of 0 means the tree is empty or uninitialized.
	/// </remarks>
	public int TreeDepth { get; }

	#endregion
	#region Methods

	/// <summary>
	/// Checks whether a specfic object is contained in this tree.
	/// </summary>
	/// <param name="_object">The object we're looking for, may not be null.</param>
	/// <returns>True if the object is contained within the tree, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Object may not be null.</exception>
	bool ContainsObject(T _object);

	/// <summary>
	/// Tries to find an object in this tree, that matches some critera.
	/// </summary>
	/// <param name="_funcSelector">A selection function that returns true for the object we're looking for. May not be null.</param>
	/// <param name="_outResult">Outputs the first object that matches the search criteria, or null, if no match was found.</param>
	/// <returns>True if a matching object was found within the tree, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Selector delegate may not be null.</exception>
	bool FindObject(Func<T, bool> _funcSelector, out T? _outResult)
	{
		ArgumentNullException.ThrowIfNull(_funcSelector);

		IEnumerable<T> allObjects = EnumerateAllObjects();
		foreach (T obj in allObjects)
		{
			if (_funcSelector(obj))
			{
				_outResult = obj;
				return true;
			}
		}

		_outResult = default;
		return false;
	}

	/// <summary>
	/// Tries to add a new object into the tree.
	/// </summary>
	/// <param name="_newObject">The new object, may not be null.</param>
	/// <returns>True if the object was added successfully, false otherwise or if the object has already been added to the tree.</returns>
	/// <exception cref="ArgumentNullException">Object may not be null.</exception>
	bool AddObject(T _newObject);

	/// <summary>
	/// Tries to remove an existing object from this tree.
	/// </summary>
	/// <param name="_object">The object we wish to remove, may not be null.</param>
	/// <returns>True if the object was found and removed successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Object may not be null.</exception>
	bool RemoveObject(T _object);

	/// <summary>
	/// Gets an enumeration of all objects within this spatial tree.
	/// </summary>
	/// <returns>An enumeration of objects.</returns>
	IEnumerable<T> EnumerateAllObjects();

	/// <summary>
	/// Gets an enumeration of all objects within this spatial tree whose bounding boxes overlap with a given region of space.
	/// </summary>
	/// <param name="_region">The region of space where we're looking for objects.</param>
	/// <returns>An enumeration of objects in that region.</returns>
	IEnumerable<T> EnumerateObjectsInRegion(AABB _region);

	/// <summary>
	/// Gets all objects within this spatial tree.
	/// </summary>
	/// <param name="_destinationList">The list where results shall be stored, may not be null.</param>
	/// <returns>True if all tree objects could be retrieved successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Destination list may not be null.</exception>
	bool GetAllObjects(List<T> _destinationList)
	{
		ArgumentNullException.ThrowIfNull(_destinationList);

		_destinationList.AddRange(EnumerateAllObjects());
		return true;
	}

	/// <summary>
	/// Gets all objects within this spatial tree whose bounding boxes overlap with a given region of space.
	/// </summary>
	/// <param name="_region">The region of space where we're looking for objects.</param>
	/// <param name="_destinationList">The list where results shall be stored, may not be null.</param>
	/// <returns>True if tree objects could be located successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Destination list may not be null.</exception>
	bool GetObjectsInRegion(in AABB _region, List<T> _destinationList)
	{
		ArgumentNullException.ThrowIfNull(_destinationList);
		
		_destinationList.AddRange(EnumerateObjectsInRegion(_region));
		return true;
	}

	#endregion
}
