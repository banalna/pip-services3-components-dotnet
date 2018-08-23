﻿using PipServices.Commons.Config;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices.Components.Cache
{
    /// <summary>
    /// Local in-memory cache that can be used in non-scaled deployments or for testing.
    /// </summary>
    /// <remarks>
    /// This class is thread-safe.
    /// </remarks>
    public class MemoryCache : AbstractCache
    {
        private readonly long DefaultTimeout = 60000;
        private const long DefaultMaxSize = 1000;

        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCache"/> class.
        /// </summary>
        public MemoryCache()
            : this (null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCache"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public MemoryCache(ConfigParams config)
        {
            Timeout = DefaultTimeout;
            MaxSize = DefaultMaxSize;

            if (config != null)
            {
                Configure(config);
            }
        }

        public long MaxSize { get; set; }

        /// <summary>
        /// Initializes the components according to supplied configuration parameters.
        /// </summary>
        /// <param name="config">Configuration parameters.</param>
        public override void Configure(ConfigParams config)
        {
            Timeout = config.GetAsLongWithDefault("timeout", Timeout);
            MaxSize = config.GetAsLongWithDefault("max_size", MaxSize);
        }

        private void Cleanup()
        {
            CacheEntry oldest = null;
            var keysToRemove = new List<string>();

            lock (_lock)
            {
                foreach (var entry in _cache)
                {
                    if (entry.Value.IsExpired())
                    {
                        keysToRemove.Add(entry.Key);
                    }
                    if (oldest == null || oldest.Expiration > entry.Value.Expiration)
                    {
                        oldest = entry.Value;
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }

                if (_cache.Count > MaxSize && oldest != null)
                {
                    _cache.Remove(oldest.Key);
                }
            }
        }

        /// <summary>
        /// Retrieves a value from cache by unique key.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="key">Unique key identifying a data object.</param>
        /// <returns>Cached value or null if the value is not found.</returns>
        public async override Task<T> RetrieveAsync<T>(string correlationId, string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            await Task.Delay(0);

            lock (_lock)
            {
                CacheEntry entry;
                if (_cache.TryGetValue(key, out entry))
                {
                    if (entry.IsExpired())
                    {
                        _cache.Remove(key);
                        return default(T);
                    }

                    return (T)entry.Value;
                }

                return default(T);
            }
        }

        /// <summary>
        /// Stores an object identified by a unique key in cache.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="key">Unique key identifying a data object.</param>
        /// <param name="value">The data object to store.</param>
        /// <param name="timeout">Time to live for the object in milliseconds.</param>
        /// <returns>Cached object stored in the cache.</returns>
        public async override Task<T> StoreAsync<T>(string correlationId, string key, T value, long timeout)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (_lock)
            {
                CacheEntry entry;
                _cache.TryGetValue(key, out entry);
                timeout = timeout > 0 ? timeout : Timeout;

                //if (value == default(T))
                //{
                //    if (entry != null)
                //        _cache.Remove(key);
                //    return default(T);
                //}

                if (entry != null)
                    entry.SetValue(value, timeout);
                else
                    _cache[key] = new CacheEntry(key, value, timeout);

                // cleanup
                if (MaxSize > 0 && _cache.Count > MaxSize)
                    Cleanup();
            }

            return await Task.FromResult(value);
        }

        /// <summary>
        /// Removes an object from cache.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="key">Unique key identifying the object.</param>
        public async override Task RemoveAsync(string correlationId, string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (_lock)
            {
                _cache.Remove(key);
            }

            await Task.Delay(0);
        }

        public async Task ClearAsync(string correlationId)
        {
            lock (_lock)
            {
                _cache.Clear();
            }

            await Task.Delay(0);
        }
    }
}