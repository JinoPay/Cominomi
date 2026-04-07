using System.Text.Json.Serialization;

namespace Seoro.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentType
{
    Code,
    Explore,
    Plan
}