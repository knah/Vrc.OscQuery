using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeaMod.DNS.Model;
using MeaMod.DNS.Multicast;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vrc.OscQuery.Zeroconf
{
    public class MeaModDiscovery : IDiscovery
    {
        private readonly ServiceDiscovery _discovery;
        private readonly MulticastService _mdns;
        private readonly ILogger Logger;
        private readonly CancellationTokenSource _expiryTokenWorkerSource = new();
        
        // Store discovered services
        private readonly MruCache<OscQueryServiceProfile> _oscServices;

        public IEnumerable<OscQueryServiceProfile> GetOscQueryServices() => _oscServices.Items.Where(it => it.Type == OscQueryServiceProfile.ServiceType.OscQuery);
        public IEnumerable<OscQueryServiceProfile> GetOscServices() => _oscServices.Items.Where(it => it.Type == OscQueryServiceProfile.ServiceType.Osc);

        public void Dispose()
        {
            _expiryTokenWorkerSource.Dispose();
            
            foreach (var (key, value) in _profiles) 
                _discovery.Unadvertise(value);
            _profiles.Clear();
            
            _discovery.Dispose();
            _mdns.Stop();
        }

        public MeaModDiscovery(ILogger<MeaModDiscovery>? logger = null)
        {
            Logger = logger ?? new NullLogger<MeaModDiscovery>();
            _oscServices = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _oscServices.StartExpiryChecking(Logger, _expiryTokenWorkerSource.Token).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Logger.LogError(task.Exception, "MRU Cleaner Worker failed with exception in MeaModDiscovery");
            });
            
            _mdns = new MulticastService();
            _mdns.UseIpv6 = false;
            _mdns.IgnoreDuplicateMessages = true;
            
            _discovery = new ServiceDiscovery(_mdns);
            
            // Query for OSC and OSCQuery services on every network interface
            _mdns.NetworkInterfaceDiscovered += (s, e) =>
            {
                RefreshServices();
            };
            
            // Callback invoked when the above query is answered
            _mdns.AnswerReceived += OnRemoteServiceInfo;
            _mdns.Start();

            RefreshLoop().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Logger.LogError(task.Exception, "mDNS refresh loop failed with exception in MeaModDiscovery");
            });
        }

        private async Task RefreshLoop()
        {
            while (!_expiryTokenWorkerSource.IsCancellationRequested)
            {
                foreach (var (_, value) in _profiles) 
                    _discovery.Announce(value);
                RefreshServices();
                
                // max expiry is 1 minute, so query twice as often
                await Task.Delay(TimeSpan.FromSeconds(30), _expiryTokenWorkerSource.Token);
            }
        }
        
        public void RefreshServices()
        {
            _mdns.SendQuery(OscQueryService.LocalOscUdpServiceName);
            _mdns.SendQuery(OscQueryService.LocalOscJsonServiceName);
        }

        private event Action<OscQueryServiceProfile>? OnAnyOscServiceAdded;
        private event Action<OscQueryServiceProfile>? OnOscServiceAdded;
        private event Action<OscQueryServiceProfile>? OnOscQueryServiceAdded;

        event Action<OscQueryServiceProfile> IDiscovery.OnOscServiceAdded
        {
            add
            {
                OnOscServiceAdded += value;
                foreach (var profile in GetOscServices()) value.Invoke(profile);
            }
            remove => OnOscServiceAdded -= value;
        }
        
        event Action<OscQueryServiceProfile> IDiscovery.OnOscQueryServiceAdded
        {
            add
            {
                OnOscQueryServiceAdded += value;
                foreach (var profile in GetOscQueryServices()) value.Invoke(profile);
            }
            remove => OnOscQueryServiceAdded -= value;
        }
        
        event Action<OscQueryServiceProfile> IDiscovery.OnAnyOscServiceAdded
        {
            add
            {
                OnAnyOscServiceAdded += value;
                foreach (var profile in _oscServices.Items) value.Invoke(profile);
            }
            remove => OnAnyOscServiceAdded -= value;
        }

        public event Action<OscQueryServiceProfile>? OnAnyOscServiceRemoved
        {
            add => _oscServices.OnExpiry += value;
            remove => _oscServices.OnExpiry -= value;
        }

        private ConcurrentDictionary<OscQueryServiceProfile, ServiceProfile> _profiles = new();
        public void Advertise(OscQueryServiceProfile profile)
        {
            var meaProfile = new ServiceProfile(profile.Name, profile.GetServiceTypeString(), (ushort)profile.Port, new[] { profile.Address });
            _discovery.Advertise(meaProfile);
            _discovery.Announce(meaProfile);
            
            if (_profiles.TryAdd(profile, meaProfile))
                Logger.LogInformation("Advertising Service {Name} of type {Type} on {Port}", profile.Name,profile.Type, profile.Port);
            else
                Logger.LogWarning("Duplicate Advertise call for service {Name} of type {Type} on {Port}", profile.Name,profile.Type, profile.Port);
        }

        public void Unadvertise(OscQueryServiceProfile profile)
        {
            if (_profiles.TryGetValue(profile, out var serviceProfile))
            {
                _discovery.Unadvertise(serviceProfile);
                _profiles.TryRemove(profile, out _);
            }
            Logger.LogInformation($"Unadvertising Service {profile.Name} of type {profile.Type} on {profile.Port}");
        }

        /// <summary>
        /// Callback invoked when an mdns Service provides information about itself 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs">Event Data with info from queried Service</param>
        private void OnRemoteServiceInfo(object? sender, MessageEventArgs eventArgs)
        {
            var response = eventArgs.Message;
            
            try
            {
                // Check whether this service matches OSCJSON or OSC services for which we're looking
                var matchingRecord = response.Answers.FirstOrDefault(record => OscQueryService.MatchedNames.Contains(record.CanonicalName));
                if (matchingRecord == null) return;

                try
                {
                    foreach (var record in response.AdditionalRecords.OfType<SRVRecord>())
                        AddMatchedService(response, matchingRecord, record);
                }
                catch (Exception)
                {
                    Logger.LogInformation($"no SRV Records found in not parse answer from {eventArgs.RemoteEndPoint}");
                }
            }
            catch (Exception e)
            {
                // Using a non-error log level because we may have just found a non-matching service
                Logger.LogInformation($"Could not parse answer from {eventArgs.RemoteEndPoint}: {e.Message}");
            }
        }

        private void AddMatchedService(Message response, ResourceRecord resourceRecord, SRVRecord srvRecord)
        {
            // Get the rest of the items we need to track this service
            var port = srvRecord.Port;
            var domainName = srvRecord.Name.Labels;
            var instanceName = domainName[0];

            var ttl = resourceRecord.TTL;

            var serviceName = string.Join(".", domainName.Skip(1));
            var ips = response.AdditionalRecords.OfType<ARecord>().Select(r => r.Address);
                
            var ipAddressList = ips.ToList();

            var serviceType = serviceName switch
            {
                OscQueryService.LocalOscJsonServiceName => OscQueryServiceProfile.ServiceType.OscQuery,
                OscQueryService.LocalOscUdpServiceName => OscQueryServiceProfile.ServiceType.Osc,
                _ => OscQueryServiceProfile.ServiceType.Unknown,
            };

            if (serviceType == OscQueryServiceProfile.ServiceType.Unknown) return;

            foreach (var ipAddress in ipAddressList)
            {
                var serviceProfile = new OscQueryServiceProfile(instanceName, ipAddress, port, serviceType);
                if (!_oscServices.AddOrTouch(serviceProfile, ttl)) continue;
                
                var callback = serviceType == OscQueryServiceProfile.ServiceType.Osc
                    ? OnOscServiceAdded
                    : OnOscQueryServiceAdded;
                callback?.Invoke(serviceProfile);
                OnAnyOscServiceAdded?.Invoke(serviceProfile);
            }
        }
    }
}