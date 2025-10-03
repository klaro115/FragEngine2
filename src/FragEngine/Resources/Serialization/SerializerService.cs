using FragEngine.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace FragEngine.Resources.Serialization;

/// <summary>
/// A service for serializaing and deserializing data.
/// </summary>
/// <param name="_logger">The engine's logging service singleton.</param>
/// <param name="_jsonOptions">JSON serialization options.</param>
public sealed class SerializerService(ILogger _logger, JsonSerializerOptions _jsonOptions)
{
	#region Fields

	private readonly ILogger logger = _logger;

	#endregion
	#region Properties

	/// <summary>
	/// Gets a standard set of JSON serialization options that should be used consistently across the app.
	/// </summary>
	public JsonSerializerOptions JsonOptions { get; } = _jsonOptions ?? throw new ArgumentNullException(nameof(_jsonOptions));

	#endregion
	#region Methods JSON

	// JSON SERIALIZATION:

	[RequiresUnreferencedCode("JSON serialization without type info requires unreferenced code, which may be trimmed!")]
	[RequiresDynamicCode("JSON serialization without type info requires dynamic code, which may be trimmed!")]
	public bool SerializeToJson<T>(T _data, out string _outJson) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_data);

		try
		{
			_outJson = JsonSerializer.Serialize(_data, JsonOptions);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to serialize object of type '{typeof(T).Name}' to JSON string!", ex, LogEntrySeverity.Normal);
			_outJson = string.Empty;
			return false;
		}
	}

	public bool SerializeToJson<T>(T _data, JsonTypeInfo<T> _typeInfo, out string _outJson) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_data);
		ArgumentNullException.ThrowIfNull(_typeInfo);

		try
		{
			_outJson = JsonSerializer.Serialize(_data, _typeInfo);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to serialize object of type '{typeof(T).Name}' to JSON string!", ex, LogEntrySeverity.Normal);
			_outJson = string.Empty;
			return false;
		}
	}

	[RequiresUnreferencedCode("JSON serialization without type info requires unreferenced code, which may be trimmed!")]
	[RequiresDynamicCode("JSON serialization without type info requires dynamic code, which may be trimmed!")]
	public bool SerializeToJson<T>(T _data, Stream _jsonStream) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_data);
		ArgumentNullException.ThrowIfNull(_jsonStream);

		try
		{
			JsonSerializer.Serialize(_jsonStream, _data, JsonOptions);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to serialize object of type '{typeof(T).Name}' to JSON stream!", ex, LogEntrySeverity.Normal);
			return false;
		}
	}

	public bool SerializeToJson<T>(T _data, JsonTypeInfo<T> _typeInfo, Stream _jsonStream) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_data);
		ArgumentNullException.ThrowIfNull(_typeInfo);
		ArgumentNullException.ThrowIfNull(_jsonStream);

		try
		{
			JsonSerializer.Serialize(_jsonStream, _data, _typeInfo);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to serialize object of type '{typeof(T).Name}' to JSON stream!", ex, LogEntrySeverity.Normal);
			return false;
		}
	}

	// JSON DESERIALIZATION:

	[RequiresUnreferencedCode("JSON deserialization without type info requires unreferenced code, which may be trimmed!")]
	[RequiresDynamicCode("JSON deserialization without type info requires dynamic code, which may be trimmed!")]
	public bool DeserializeFromJson<T>(string _json, out T? _outObject)
	{
		ArgumentNullException.ThrowIfNull(_json);

		try
		{
			_outObject = JsonSerializer.Deserialize<T>(_json, JsonOptions);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to deserialize object of type '{typeof(T).Name}' from JSON string!", ex, LogEntrySeverity.Normal);
			_outObject = default;
			return false;
		}
	}

	public bool DeserializeFromJson<T>(string _json, JsonTypeInfo<T> _typeInfo, out T? _outObject)
	{
		ArgumentNullException.ThrowIfNull(_json);
		ArgumentNullException.ThrowIfNull(_typeInfo);

		try
		{
			_outObject = JsonSerializer.Deserialize(_json, _typeInfo);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to deserialize object of type '{typeof(T).Name}' from JSON string!", ex, LogEntrySeverity.Normal);
			_outObject = default;
			return false;
		}
	}

	[RequiresUnreferencedCode("JSON deserialization without type info requires unreferenced code, which may be trimmed!")]
	[RequiresDynamicCode("JSON deserialization without type info requires dynamic code, which may be trimmed!")]
	public bool DeserializeFromJson<T>(Stream _jsonStream, out T? _outObject)
	{
		ArgumentNullException.ThrowIfNull(_jsonStream);

		try
		{
			_outObject = JsonSerializer.Deserialize<T>(_jsonStream, JsonOptions);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to deserialize object of type '{typeof(T).Name}' from JSON stream!", ex, LogEntrySeverity.Normal);
			_outObject = default;
			return false;
		}
	}

	public bool DeserializeFromJson<T>(Stream _jsonStream, JsonTypeInfo<T> _typeInfo, out T? _outObject)
	{
		ArgumentNullException.ThrowIfNull(_jsonStream);
		ArgumentNullException.ThrowIfNull(_typeInfo);

		try
		{
			_outObject = JsonSerializer.Deserialize(_jsonStream, _typeInfo);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogException($"Failed to deserialize object of type '{typeof(T).Name}' from JSON stream!", ex, LogEntrySeverity.Normal);
			_outObject = default;
			return false;
		}
	}

	#endregion
}
