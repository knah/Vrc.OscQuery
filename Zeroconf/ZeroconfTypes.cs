using System;
using System.Collections.Generic;

namespace Vrc.OscQuery.Zeroconf
{
    public interface IDiscovery : IDisposable
    {
        void RefreshServices();
        event Action<OscQueryServiceProfile> OnAnyOscServiceAdded;
        event Action<OscQueryServiceProfile> OnOscServiceAdded;
        event Action<OscQueryServiceProfile> OnOscQueryServiceAdded;
        event Action<OscQueryServiceProfile> OnAnyOscServiceRemoved;
        IEnumerable<OscQueryServiceProfile> GetOscQueryServices();
        IEnumerable<OscQueryServiceProfile> GetOscServices();
        
        void Advertise(OscQueryServiceProfile profile);
        void Unadvertise(OscQueryServiceProfile profile);
    }
}