namespace FragEngine.Scenes;

/// <summary>
/// Interface for objects that have some form of a size and location in the physical space of a scene.
/// </summary>
public interface IPhysicalObject
{
	#region Methods

	/// <summary>
	/// Gets or calculates a bounding box volume that fully encloses this object.
	/// </summary>
	/// <returns>The object's AABB.</returns>
	public AABB GetBoundingBox();

	#endregion
}
