using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ceen;

namespace Vrc.OscQuery
{
    internal static class FrameworkCompat
    {
#if !NET8_0_OR_GREATER
        public static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken _) => content.ReadAsStreamAsync();


        public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null) where T:IComparable<T>
        {
            if (value.CompareTo(other) > 0)
            {
                throw new ArgumentOutOfRangeException(paramName, value, $"Parameter {paramName} must be greater than {other}");
            }
        }

        public static void Deconstruct<TK, TV>(this KeyValuePair<TK, TV> kvp, out TK key, out TV value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        public static TV? GetValueOrDefault<TK, TV>(this IDictionary<TK, TV> dictionary, TK key,
            TV? defaultValue = default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static bool SetResponseNotFound(this IHttpContext context, string? text = null)
        {
            return context.SetResponseStatus(HttpStatusCode.NotFound, text);
        }
#else
        public static void ThrowIfGreaterThan<T>(T value, T other,
            [CallerArgumentExpression(nameof(value))] string? paramName = null) where T : IComparable<T> =>
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, other, paramName);
#endif
    }
}

#if !NET8_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal class RequiredMemberAttribute : Attribute;

    internal class IsExternalInit : Attribute;
    
    internal class CompilerFeatureRequiredAttribute(string featureName) : Attribute
    {
        public string FeatureName { get; } = featureName;

        /// <summary>
        /// If true, the compiler can choose to allow access to the location where this attribute is applied if it does not understand <see cref="FeatureName"/>.
        /// </summary>
        public bool IsOptional { get; init; }
    }

    internal class CallerArgumentExpressionAttribute(string parameterName) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    internal class SetsRequiredMembersAttribute : Attribute;
}
#endif