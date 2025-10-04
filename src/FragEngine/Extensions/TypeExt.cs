namespace FragEngine.Extensions;

/// <summary>
/// Extension methods for the <see cref="Type"/> class.
/// </summary>
public static class TypeExt
{
	#region Methods

	/// <summary>
	/// Whether this type is a static class.
	/// </summary>
	/// <param name="_type">This type.</param>
	/// <returns>True if it's a static class type, false otherwise.</returns>
	public static bool IsStatic(this Type _type)
	{
		bool isStatic = _type.IsClass && _type.IsSealed && _type.IsAbstract;
		return isStatic;
	}

	#endregion
}
