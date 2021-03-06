﻿using System;

namespace PipServices3.Components.Lock
{
    /// <summary>
    /// Interface for locks to synchronize work or parallel processes and to prevent collisions.
    /// 
    /// The lock allows to manage multiple locks identified by unique keys.
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// Makes a single attempt to acquire a lock by its key.
        /// It returns immediately a positive or negative result.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="key">a unique lock key to acquire.</param>
        /// <param name="ttl">a lock timeout (time to live) in milliseconds.</param>
        /// <returns>a lock result</returns>
        bool TryAcquireLock(string correlationId, string key, long ttl);

        /// <summary>
        /// Makes multiple attempts to acquire a lock by its key within give time interval.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="key">a unique lock key to acquire.</param>
        /// <param name="ttl">a lock timeout (time to live) in milliseconds.</param>
        /// <param name="timeout">a lock acquisition timeout.</param>
        void AcquireLock(string correlationId, string key, long ttl, long timeout);

        /// <summary>
        /// Releases prevously acquired lock by its key.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="key">a unique lock key to acquire.</param>
        void ReleaseLock(string correlationId, string key);
    }
}
