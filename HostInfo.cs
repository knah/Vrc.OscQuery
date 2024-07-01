using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Vrc.OscQuery
{
    public record HostInfo
    {
        [JsonPropertyName(Keys.NAME)]
        public required string Name;
        
        [JsonPropertyName(Keys.EXTENSIONS)]
        public Dictionary<string, bool> Extensions = new()
        {
            { Attributes.ACCESS, true },
            { Attributes.CLIPMODE, false },
            { Attributes.RANGE, true },
            { Attributes.TYPE, true },
            { Attributes.VALUE, true },
        };
        
        [JsonPropertyName(Keys.OSC_IP)]
        public required string OscIp;
        
        [JsonPropertyName(Keys.OSC_PORT)]
        public int OscPort = 0;
        
        [JsonPropertyName(Keys.OSC_TRANSPORT)]
        public string OscTransport = Keys.OSC_TRANSPORT_UDP;

        [JsonIgnore]
        public IPEndPoint OscEndPoint => new IPEndPoint(IPAddress.Parse(OscIp), OscPort);

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, GeneratedJsonSerializers.Default.HostInfo);
        }

        public static HostInfo? FromJson(string json)
        {
            return JsonSerializer.Deserialize(json, GeneratedJsonSerializers.Default.HostInfo);
        }

        public static ValueTask<HostInfo?> FromJsonAsync(Stream json, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.DeserializeAsync(json, GeneratedJsonSerializers.Default.HostInfo, cancellationToken);
        }

        public static class Keys
        {
            public const string NAME = "NAME";
            public const string EXTENSIONS = "EXTENSIONS";
            public const string OSC_IP = "OSC_IP";
            public const string OSC_PORT = "OSC_PORT";
            public const string OSC_TRANSPORT = "OSC_TRANSPORT";
            public const string OSC_TRANSPORT_UDP = "UDP";
        }
    }
}