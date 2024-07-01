using System;
using System.Collections.Generic;

namespace Vrc.OscQuery
{
    public static class Attributes
    {
        [Flags]
        public enum AccessValues
        {
            NoValue = 0,
            Read = 1,
            Write = 2,
            ReadWrite = 3
        }

        private static readonly Dictionary<Type, string> OscTypeLookup = new Dictionary<Type, string>()
        {
            {typeof(int), "i"},
            {typeof(uint), "u"},
            {typeof(long), "h"},
            {typeof(float), "f"},
            {typeof(double), "d"},
            {typeof(string), "s"},
            {typeof(char), "c"},
            {typeof(Array), "[,]"},
            {typeof(byte[]), "b"},
            {typeof(bool), "T"},
        };

        // Todo: handle array types
        public static bool OscTypeFor(Type type, out string oscType)
        {
            if (OscTypeLookup.TryGetValue(type, out var value))
            {
                oscType = value;
                return true;
            }

            oscType = string.Empty;
            return false;
        }
        
        #region Required Attributes

        public const string CONTENTS = "CONTENTS";
        public const string HOST_INFO = "HOST_INFO";
        public const string FULL_PATH = "FULL_PATH";
        public const string TYPE = "TYPE";

        #endregion

        #region Optional Attributes

        public const string ACCESS = "ACCESS";
        public const string CLIPMODE = "CLIPMODE";
        public const string CRITICAL = "CRITICAL";
        public const string DESCRIPTION = "DESCRIPTION";
        public const string EXTENDED_TYPE = "EXTENDED_TYPE";
        public const string HTML = "HTML";
        public const string OVERLOADS = "OVERLOADS";
        public const string RANGE = "RANGE";
        public const string TAGS = "TAGS";
        public const string UNIT = "UNIT";
        public const string VALUE = "VALUE";

        #endregion
        
        #region Service Types
        
        public const string SERVICE_OSCJSON_TCP = "_oscjson._tcp";
        public const string SERVICE_OSC_UDP = "_osc._udp";
        
        #endregion
    }
}