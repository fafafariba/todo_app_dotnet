using System.Text.Json.Serialization;

namespace Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    LOW,
    MEDIUM,
    HIGH
}
