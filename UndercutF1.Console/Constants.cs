using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace UndercutF1.Console;

/// <summary>
/// Common constants for UndercutF1
/// </summary>
public static class Constants
{
    /// <summary>
    /// Standard JSON serializer options for dealing with Timing Data and configuration.
    /// </summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
}
