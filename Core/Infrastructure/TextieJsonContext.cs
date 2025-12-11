using System.Collections.Generic;
using System.Text.Json.Serialization;
using Textie.Core.Configuration;
using Textie.Core.Scheduling;

namespace Textie.Core.Infrastructure
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonSerializable(typeof(SpamConfiguration))]
    [JsonSerializable(typeof(List<SpamProfile>))]
    [JsonSerializable(typeof(List<ScheduledRun>))]
    internal partial class TextieJsonContext : JsonSerializerContext
    {
    }
}
