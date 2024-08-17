using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vrc.OscQuery.Zeroconf;

namespace Vrc.OscQuery
{
    public class OscQueryService : IDisposable
    {
        public int TcpPort { get; private set; }
        public IPAddress HostIp { get; }

        public ILogger<OscQueryService> Logger { get; }

        // Services
        public const string LocalOscUdpServiceName = $"{Attributes.SERVICE_OSC_UDP}.local";
        public const string LocalOscJsonServiceName = $"{Attributes.SERVICE_OSCJSON_TCP}.local";
        
        public static readonly HashSet<string> MatchedNames = [LocalOscUdpServiceName, LocalOscJsonServiceName];
        
        public IDiscovery Discovery { get; }

        // HTTP Server
        private OscQueryHttpServer? myHttp;
        
        public HostInfo HostInfo { get; }

        // Lazy RootNode
        private OscQueryRootNode? myRootNode;
        public OscQueryRootNode RootNode => myRootNode ??= BuildRootNode();

        public OscQueryService StartHttpServer(int portNumber = 0, bool advertise = true)
        {
            if (myHttp != null)
                throw new InvalidOperationException($"HTTP server is already running on port {TcpPort}");
            FrameworkCompat.ThrowIfGreaterThan(portNumber, ushort.MaxValue);

            TcpPort = portNumber <= 0 ? Utils.GetAvailableTcpPort() : portNumber;
            myHttp = new OscQueryHttpServer(this, Logger);
            
            if (advertise)
                AdvertiseOscQueryService();

            return this;
        }
        
        public OscQueryService AdvertiseOscQueryService()
        {
            if (TcpPort <= 0)
                throw new InvalidOperationException("Can't advertise OSCQuery service before it's started!");
            Discovery.Advertise(new OscQueryServiceProfile(HostInfo.Name, HostIp, TcpPort, OscQueryServiceProfile.ServiceType.OscQuery));

            return this;
        }

        public OscQueryService AdvertiseOscService(int oscPort)
        {
            if (oscPort is <= 0 or > ushort.MaxValue) 
                throw new ArgumentOutOfRangeException(nameof(oscPort));
            if (HostInfo.OscPort != 0)
                throw new InvalidOperationException(
                    $"Already advertising an OSC service on {HostInfo.OscPort}; multiple advertisements are not supported!");
            
            HostInfo.OscPort = oscPort;
            Discovery.Advertise(new OscQueryServiceProfile(HostInfo.Name, HostIp, oscPort, OscQueryServiceProfile.ServiceType.Osc));

            return this;
        }

        public void RefreshServices()
        {
            Discovery.RefreshServices();
        }

        public void SetValue(string address, string value)
        {
            var target = RootNode.GetNodeWithPath(address) ?? RootNode.AddNode(new OscQueryNode(address));
            target.Value = new object[] { value };
        }
        
        public void SetValue(string address, object[] value)
        {
            var target = RootNode.GetNodeWithPath(address) ?? RootNode.AddNode(new OscQueryNode(address));
            target.Value = value;
        }

        public OscQueryService WithEndpoint(string path, string oscTypeString, Attributes.AccessValues accessValues,
            object[]? initialValue = null, string description = "")
        {
            AddEndpoint(path, oscTypeString, accessValues, initialValue, description);
            return this;
        }

        /// <summary>
        /// Registers the info for an OSC path.
        /// </summary>
        /// <param name="path">Full OSC path to entry</param>
        /// <param name="oscTypeString">String representation of OSC type(s)</param>
        /// <param name="accessValues">Enum - 0: NoValue, 1: ReadOnly 2:WriteOnly 3:ReadWrite</param>
        /// <param name="initialValue">Starting value for param in string form</param>
        /// <param name="description">Optional longer string to use when displaying a label for the entry</param>
        /// <returns></returns>
        public bool AddEndpoint(string path, string oscTypeString, Attributes.AccessValues accessValues, object[]? initialValue = null, string description = "")
        {
            // Exit early if path does not start with slash
            if (!path.StartsWith("/"))
            {
                Logger.LogError($"An OSC path must start with a '/', your path {path} does not.");
                return false;
            }
            
            if (RootNode.GetNodeWithPath(path) != null)
            {
                Logger.LogWarning($"Path already exists, skipping: {path}");
                return false;
            }
            
            RootNode.AddNode(new OscQueryNode(path)
            {
                Access = accessValues,
                Description = description,
                OscType = oscTypeString,
                Value = initialValue
            });
            
            return true;
        }
        
        public bool AddEndpoint<T>(string path, Attributes.AccessValues accessValues, object[]? initialValue = null, string description = "")
        {
            var typeExists = Attributes.OscTypeFor(typeof(T), out var oscType);

            if (typeExists) return AddEndpoint(path, oscType, accessValues, initialValue, description);
            
            Logger.LogError($"Could not add {path} to OSCQueryService because type {typeof(T)} is not supported.");
            return false;
        }

        /// <summary>
        /// Removes the data for a given OSC path, including its value getter if it has one
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveEndpoint(string path)
        {
            // Exit early if no matching path is found
            if (RootNode.GetNodeWithPath(path) == null)
            {
                Logger.LogWarning($"No endpoint found for {path}");
                return false;
            }

            RootNode.RemoveNode(path);

            return true;
        }
        
        /// <summary>
        /// Constructs the response the server will use for HOST_INFO queries
        /// </summary>
        private static OscQueryRootNode BuildRootNode()
        {
            return new OscQueryRootNode
            {
                Access = Attributes.AccessValues.NoValue,
                Description = "root node",
                FullPath = "/",
            };
        }

        public void Dispose()
        {
            myHttp?.Dispose();
            Discovery?.Dispose();

            GC.SuppressFinalize(this);
        }

        ~OscQueryService()
        {
           Dispose();
        }
        
        public OscQueryService(string serviceName, IPAddress hostIp, IDiscovery? discovery = null, ILoggerFactory? logger = null)
        {
            Logger = logger?.CreateLogger<OscQueryService>() ?? NullLogger<OscQueryService>.Instance;
            Discovery = discovery ?? new MeaModDiscovery(logger?.CreateLogger<MeaModDiscovery>());

            HostIp = hostIp;

            HostInfo = new HostInfo
            {
                Name = serviceName,
                OscIp = HostIp.ToString(),
            };
        }
    }

}