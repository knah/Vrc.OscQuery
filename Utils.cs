using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Vrc.OscQuery
{
    public static class Utils
    {
        private static readonly HttpClient Client = new();

        private static readonly IPEndPoint DefaultLoopbackEndpoint = new(IPAddress.Loopback, port: 0);
        
        public static int GetAvailableTcpPort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(DefaultLoopbackEndpoint);
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }
        
        public static int GetAvailableUdpPort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(DefaultLoopbackEndpoint);
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        public static async Task<OscQueryRootNode?> GetOscTree(IPAddress ip, int port, string? address = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address))
                address = "/";
            var response = await Client.GetAsync($"http://{ip}:{port}{(address.StartsWith("/") ? "" : "/")}{address}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await OscQueryRootNode.FromStreamAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken);
        }

        public static async Task<HostInfo?> GetHostInfo(IPAddress address, int port, CancellationToken cancellationToken = default)
        {
            var response = await Client.GetAsync($"http://{address}:{port}?{Attributes.HOST_INFO}", cancellationToken);
            return await HostInfo.FromJsonAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken);
        }
    }
}