namespace FragEngine.EngineCore.Enums;

/// <summary>
/// Enumeration of different operating systems.
/// </summary>
public enum OperatingSystemType
{
	// DESKTOP:

	/// <summary>
	/// Microsoft Windows desktop OS.
	/// </summary>
	Window,
	/// <summary>
	/// Apple MacOS desktop OS.
	/// </summary>
	MacOS,
	/// <summary>
	/// Some Linux/Posix desktop OS.
	/// </summary>
	Linux,
	/// <summary>
	/// Some OpenBSD or FreeBSD desktop OS.
	/// </summary>
	BSD,

	// MOBILE:

	/// <summary>
	/// Apple's OS for mobile phones (and tablets).
	/// </summary>
	iOS,
	/// <summary>
	/// Android OS for mobile phones and tablets.
	/// </summary>
	Android,
	//...
}

/// <summary>
/// Extension methods for the <see cref="OperatingSystemType"/> enum.
/// </summary>
public static class OperatingSystemTypeExt
{
	#region Methods

	/// <summary>
	/// Gets whether this OS type is likely running on a desktop computer.
	/// </summary>
	/// <param name="_osType">This OS type.</param>
	/// <returns>True for a desktop OS, false for console or mobile.</returns>
	public static bool IsDesktopOS(this OperatingSystemType _osType)
	{
		return _osType switch
		{
			OperatingSystemType.Window => true,
			OperatingSystemType.MacOS => true,
			OperatingSystemType.Linux => true,
			OperatingSystemType.BSD => true,
			_ => false,
		};
	}

	/// <summary>
	/// Gets whether this OS type is likely running on a mobile phone or tablet.
	/// </summary>
	/// <param name="_osType">This OS type.</param>
	/// <returns>True for a mobile OS, false for console or desktop.</returns>
	public static bool IsMobileOS(this OperatingSystemType _osType)
	{
		return _osType switch
		{
			OperatingSystemType.iOS => true,
			OperatingSystemType.Android => true,
			_ => false,
		};
	}

	#endregion
}
