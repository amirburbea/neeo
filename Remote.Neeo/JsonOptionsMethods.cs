using System.Text.Json;
using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    public static class JsonOptionsMethods
    {
        public static JsonSerializerOptions ApplyBrainSettings(this JsonSerializerOptions options)
        {
            options.DictionaryKeyPolicy = options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            return options;
        }
    }
}
