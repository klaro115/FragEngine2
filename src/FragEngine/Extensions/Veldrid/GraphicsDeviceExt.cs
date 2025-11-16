using System.Collections.Frozen;
using Veldrid;

namespace FragEngine.Extensions.Veldrid;

/// <summary>
/// Extension methods for the <see cref="GraphicsDevice"/> class.
/// </summary>
public static class GraphicsDeviceExt
{
	#region Fields

	private static readonly FrozenDictionary<int, string> vendorCompanyNameMap = new Dictionary<int, string>()
	{
		[ 0x1002 ] = "AMD",
		[ 0x102B ] = "Matrox",
		[ 0x106B ] = "Apple",
		[ 0x10DE ] = "Nvidia",
		[ 0x13B5 ] = "ARM",
		[ 0x14C3 ] = "Mediatek",
		[ 0x5143 ] = "Qualcomm",
		[ 0x8086 ] = "Intel",
		//...
	}.ToFrozenDictionary();

	private const string vendorNameIdPrefix = "id:";

	#endregion
	#region Methods

	/// <summary>
	/// Tries to parse the device's vendor ID, to retrieve the name of the actual manufacturer company.
	/// </summary>
	/// <param name="_device">This graphics device.</param>
	/// <param name="_outVendorName">Outputs the name of the vendor company, or empty if the ID could not be parsed, or "Unknown" if the vendor is not known.</param>
	/// <returns>True if the name of the vendor company could be identified, false on failure to parse or if the vendor is unknown.</returns>
	/// <exception cref="ArgumentNullException">Graphics device may not be null.</exception>
	public static bool TryGetVendorCompanyName(this GraphicsDevice _device, out string _outVendorName)
	{
		ArgumentNullException.ThrowIfNull(_device);

		string vendorName = _device.VendorName;
		if (string.IsNullOrEmpty(vendorName) || vendorName.Length <= vendorNameIdPrefix.Length)
		{
			_outVendorName = string.Empty;
			return false;
		}

		string vendorIdTxt = vendorName[vendorNameIdPrefix.Length..];
		if (!int.TryParse(vendorIdTxt, System.Globalization.NumberStyles.HexNumber, null, out int vendorId))
		{
			_outVendorName = string.Empty;
			return false;
		}

		if (!vendorCompanyNameMap.TryGetValue(vendorId, out string? companyName))
		{
			_outVendorName = "Unknown";
			return false;
		}

		_outVendorName = companyName;
		return true;
	}

	#endregion
}
