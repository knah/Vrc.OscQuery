using System.Text.Json.Serialization;

namespace Vrc.OscQuery;

[JsonSourceGenerationOptions(IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(HostInfo))]
[JsonSerializable(typeof(OscQueryNode))]
[JsonSerializable(typeof(OscQueryRootNode))]
[JsonSerializable(typeof(OscRange))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(bool))]
public partial class GeneratedJsonSerializers : JsonSerializerContext
{
    
}