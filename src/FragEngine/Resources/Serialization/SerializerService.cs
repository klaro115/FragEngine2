using FragEngine.Logging;
using System.Diagnostics;
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

	private readonly ILogger logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

	#endregion
	#region Properties

	/// <summary>
	/// Gets a standard set of JSON serialization options that should be used consistently across the app.
	/// </summary>
	public JsonSerializerOptions JsonOptions { get; } = _jsonOptions ?? throw new ArgumentNullException(nameof(_jsonOptions));

	#endregion
	#region Methods JSON

	// JSON SERIALIZATION:

	/// <summary>
	/// Serializes an object to a JSON string.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_data">The instance we wish to serialize, may not be null.</param>
	/// <param name="_outJson">Outputs a JSON string representing the object's data.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Data object may not be null.</exception>
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

	/// <summary>
	/// Serializes an object to a JSON string.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_data">The instance we wish to serialize, may not be null.</param>
	///	<param name="_typeInfo">Type information for serializing objects of this type.<para/>
	///	This type info is required to make JSON serialization safe in a trimmed/AoT-compiled application,
	///	where reflection and dynamic code may not be available.</param>
	/// <param name="_outJson">Outputs a JSON string representing the object's data.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Data object and type info may not be null.</exception>
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

	/// <summary>
	/// Serializes an object to a JSON stream.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_data">The instance we wish to serialize, may not be null.</param>
	/// <param name="_jsonStream">A stream that the serialized JSON will be written to. Must support writing.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Data object and JSON stream may not be null.</exception>
	[RequiresUnreferencedCode("JSON serialization without type info requires unreferenced code, which may be trimmed!")]
	[RequiresDynamicCode("JSON serialization without type info requires dynamic code, which may be trimmed!")]
	public bool SerializeToJson<T>(T _data, Stream _jsonStream) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_data);
		ArgumentNullException.ThrowIfNull(_jsonStream);

		Debug.Assert(_jsonStream.CanWrite, "JSON stream must support writing!");

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

	/// <summary>
	/// Serializes an object to a JSON stream.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_data">The instance we wish to serialize, may not be null.</param>
	///	<param name="_typeInfo">Type information for serializing objects of this type.<para/>
	///	This type info is required to make JSON serialization safe in a trimmed/AoT-compiled application,
	///	where reflection and dynamic code may not be available.</param>
	/// <param name="_jsonStream">A stream that the serialized JSON will be written to. Must support writing.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Data object, type info, and JSON stream may not be null.</exception>
	public bool SerializeToJson<T>(T _data, JsonTypeInfo<T> _typeInfo, Stream _jsonStream) where T : notnull
	{
		ArgumentNullException.ThrowIfNull(_data);
		ArgumentNullException.ThrowIfNull(_typeInfo);
		ArgumentNullException.ThrowIfNull(_jsonStream);

		Debug.Assert(_jsonStream.CanWrite, "JSON stream must support writing!");

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

	/// <summary>
	/// Deserializes an object to from a JSON string.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_json">A string containing JSON-formatted data.</param>
	/// <param name="_outObject">Outputs the deserialized object instance, or null, if deserialization failed.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">The JSON string may not be null.</exception>
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

	/// <summary>
	/// Deserializes an object to from a JSON string.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_json">A string containing JSON-formatted data.</param>
	///	<param name="_typeInfo">Type information for deserializing objects of this type.<para/>
	///	This type info is required to make JSON serialization safe in a trimmed/AoT-compiled application,
	///	where reflection and dynamic code may not be available.</param>
	/// <param name="_outObject">Outputs the deserialized object instance, or null, if deserialization failed.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">The JSON string and type info may not be null.</exception>
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

	/// <summary>
	/// Deserializes an object to from a JSON stream.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_jsonStream">A stream that the serialized JSON will be read from. Must support reading.</param>
	/// <param name="_outObject">Outputs the deserialized object instance, or null, if deserialization failed.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">The JSON stream may not be null.</exception>
	[RequiresUnreferencedCode("JSON deserialization without type info requires unreferenced code, which may be trimmed!")]
	[RequiresDynamicCode("JSON deserialization without type info requires dynamic code, which may be trimmed!")]
	public bool DeserializeFromJson<T>(Stream _jsonStream, out T? _outObject)
	{
		ArgumentNullException.ThrowIfNull(_jsonStream);

		Debug.Assert(_jsonStream.CanRead, "JSON stream must support reading!");

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

	/// <summary>
	/// Deserializes an object to from a JSON stream.
	/// </summary>
	/// <typeparam name="T">The type of the serialized object.</typeparam>
	/// <param name="_jsonStream">A stream that the serialized JSON will be read from. Must support reading.</param>
	///	<param name="_typeInfo">Type information for deserializing objects of this type.<para/>
	///	This type info is required to make JSON serialization safe in a trimmed/AoT-compiled application,
	///	where reflection and dynamic code may not be available.</param>
	/// <param name="_outObject">Outputs the deserialized object instance, or null, if deserialization failed.</param>
	/// <returns>True if the object was serialized successfully, false otherwise.</returns>
	/// <exception cref="ArgumentNullException">Type info and JSON stream may not be null.</exception>
	public bool DeserializeFromJson<T>(Stream _jsonStream, JsonTypeInfo<T> _typeInfo, out T? _outObject)
	{
		ArgumentNullException.ThrowIfNull(_jsonStream);
		ArgumentNullException.ThrowIfNull(_typeInfo);

		Debug.Assert(_jsonStream.CanRead, "JSON stream must support reading!");

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
