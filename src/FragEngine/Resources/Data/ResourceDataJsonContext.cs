using System.Text.Json.Serialization;

namespace FragEngine.Resources.Data;

/// <summary>
/// Serialization context for reading and writing JSON of resource data types.
/// </summary>
[JsonSerializable(typeof(ResourceData))]
[JsonSerializable(typeof(ResourceManifest))]
internal sealed partial class ResourceDataJsonContext : JsonSerializerContext;
