using System;
using System.Net;

namespace Vrc.OscQuery
{
    public sealed class OscQueryServiceProfile : IEquatable<OscQueryServiceProfile>
    {
        public readonly int Port;
        public readonly string Name;
        public readonly IPAddress Address;
        public readonly ServiceType Type;
        public readonly IPEndPoint EndPoint;

        public enum ServiceType
        {
            Unknown, OscQuery, Osc
        }

        public string GetServiceTypeString()
        {
            switch (Type)
            {
                case ServiceType.Osc:
                    return Attributes.SERVICE_OSC_UDP;
                case ServiceType.OscQuery:
                    return Attributes.SERVICE_OSCJSON_TCP;
                default:
                    return "UNKNOWN";
            }
        }

        public OscQueryServiceProfile(string name, IPAddress address, int port, ServiceType serviceType)
        {
            Name = name;
            Address = address;
            Port = port;
            Type = serviceType;
            EndPoint = new IPEndPoint(address, port);
        }

        public bool Equals(OscQueryServiceProfile? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Port == other.Port && Name == other.Name && Address.Equals(other.Address) && Type == other.Type;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OscQueryServiceProfile)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Port;
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Address.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }
        }

        public static bool operator ==(OscQueryServiceProfile? left, OscQueryServiceProfile? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OscQueryServiceProfile? left, OscQueryServiceProfile? right)
        {
            return !Equals(left, right);
        }
    }
}