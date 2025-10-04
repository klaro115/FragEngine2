using System.Text.Json.Serialization;

namespace FragEngine.EngineCore.Config;

/// <summary>
/// Serialization context for reading and writing JSON of engine core types.
/// </summary>
[JsonSerializable(typeof(EngineConfig))]
[JsonSerializable(typeof(EngineStartupConfig))]
[JsonSerializable(typeof(OptimizationsConfig))]
internal sealed partial class EngineCoreJsonContext : JsonSerializerContext;
