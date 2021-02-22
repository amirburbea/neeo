using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<ComponentType>))]
    public enum ComponentType
    {
    }
}
