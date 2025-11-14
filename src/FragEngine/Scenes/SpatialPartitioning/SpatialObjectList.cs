using System.Numerics;

namespace FragEngine.Scenes.SpatialPartitioning;

/// <summary>
/// A list of physical objects that does not use any spatial partitioning scheme.
/// This is essentially just a wrapper around a <see cref="HashSet{T}"/>, that is can be used in-lieu of a spatial tree.
/// For very simple scenes or for testing purposes, using such a simple type instead of a proper spatial partitioning
/// system can offer performance benefits.
/// </summary>
/// <remarks>
/// Note: This type may be used as a placeholder or as the default spatial partitioning scheme, in case no spatial tree
/// structure has been specfied by users of spatial partitioning.
/// </remarks>
/// <typeparam name="T">The type of physical objects in this spatial tree.</typeparam>
public sealed class SpatialObjectList<T> : HashSet<T>, ISpatialTree<T> where T : IPhysicalObject
{
	#region Fields

	private bool isDirty = false;
	private AABB bounds = AABB.Zero;

	#endregion
	#region Properties

	public int ObjectCount => Count;
	public int TreeDepth => 1;

	#endregion
	#region Methods

	public bool ContainsObject(T _object) => Contains(_object);
	public bool AddObject(T _newObject)
	{
		isDirty = true;
		return Add(_newObject);
	}

	public bool RemoveObject(T _object)
	{
		isDirty = true;
		return Remove(_object);
	}

	public IEnumerable<T> EnumerateAllObjects() => this;

	public IEnumerable<T> EnumerateObjectsInRegion(AABB _region)
	{
		foreach (T obj in this)
		{
			AABB objectBounds = obj.GetBoundingBox();
			if (_region.Overlaps(in objectBounds))
			{
				yield return obj;
			}
		}
	}

	public AABB GetBoundingBox()
	{
		if (isDirty)
		{
			RecalculateBoundingBox();
			isDirty = false;
		}

		return bounds;
	}

	private void RecalculateBoundingBox()
	{
		if (Count == 0)
		{
			bounds = AABB.Zero;
			return;
		}

		IEnumerator<T> e = GetEnumerator();
		e.MoveNext();

		AABB objectBounds = e.Current.GetBoundingBox();
		Vector3 min = objectBounds.Minimum;
		Vector3 max = objectBounds.Maximum;

		while (e.MoveNext())
		{
			objectBounds = e.Current.GetBoundingBox();
			min = Vector3.Min(objectBounds.Minimum, min);
			max = Vector3.Max(objectBounds.Maximum, min);
		}

		bounds = new AABB(min, max);
	}

	#endregion
}
