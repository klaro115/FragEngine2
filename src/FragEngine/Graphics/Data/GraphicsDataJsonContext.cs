using FragEngine.Graphics.Settings;
using System.Text.Json.Serialization;

namespace FragEngine.Graphics.Data;

/// <summary>
/// Serialization context for reading and writing JSON of graphics data types.
/// </summary>
[JsonSerializable(typeof(GraphicsSettings))]
[JsonSerializable(typeof(GraphicsConfig))]
internal sealed partial class GraphicsDataJsonContext : JsonSerializerContext;
