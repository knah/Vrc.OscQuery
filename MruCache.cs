using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vrc.OscQuery;

public class MruCache<T> where T: notnull
{
    private readonly TimeSpan myDefaultExpiry;
    private readonly TimeSpan myMaxExpiry;
    private readonly ConcurrentDictionary<T, DateTime> myDictionary;
    private readonly SemaphoreSlim myNearestExpiryChanged = new(0);
    
    private DateTime myNearestExpiry = DateTime.UtcNow;

    public event Action<T>? OnExpiry;

    public MruCache(TimeSpan defaultExpiry, TimeSpan? maxExpiry = null, IEqualityComparer<T>? comparer = null)
    {
        myMaxExpiry = maxExpiry ?? TimeSpan.MaxValue;
        myDictionary = new(comparer);
        myDefaultExpiry = defaultExpiry;
    }

    public bool AddOrTouch(T value, TimeSpan? inExpiry = null)
    {
        var expirySpan = inExpiry ?? myDefaultExpiry;
        if (expirySpan > myMaxExpiry)
            expirySpan = myMaxExpiry;
        var expiry = DateTime.UtcNow + expirySpan;
        if (expiry < myNearestExpiry)
        {
            myNearestExpiry = expiry;
            myNearestExpiryChanged.Release();
        }
        
        if (myDictionary.TryAdd(value, expiry))
            return true;
        myDictionary[value] = expiry;
        return false;
    }

    public IEnumerable<T> Items => myDictionary.Keys;

    public Task StartExpiryChecking(ILogger? logger, CancellationToken cancellationToken)
    {
        return ExpiryChecker(logger, cancellationToken);
    }

    private async Task ExpiryChecker(ILogger? logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var waitTimeout = myNearestExpiry - DateTime.UtcNow;
            if (waitTimeout < TimeSpan.Zero) 
                waitTimeout = myDictionary.IsEmpty ? myDefaultExpiry : TimeSpan.Zero;
            if (waitTimeout > myDefaultExpiry)
                waitTimeout = myDefaultExpiry;
            await myNearestExpiryChanged.WaitAsync(waitTimeout, cancellationToken);
            CheckExpiry(logger);
        }
    }

    private void CheckExpiry(ILogger? logger)
    {
        var now = DateTime.UtcNow;
        var newNearestExpiry = DateTime.MaxValue;
        foreach (var (key, value) in myDictionary)
        {
            if (value > now)
            {
                if (value < newNearestExpiry) newNearestExpiry = value;
                continue;
            }
            if (!myDictionary.TryRemove(key, out _)) continue;
            
            try
            {
                OnExpiry?.Invoke(key);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception while processing MRU expiry callback for {Item}", key);
            }
        }

        myNearestExpiry = newNearestExpiry;
    }
}