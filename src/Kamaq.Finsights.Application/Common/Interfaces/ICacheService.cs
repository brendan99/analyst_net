using System;
using System.Threading.Tasks;

namespace Kamaq.Finsights.Application.Common.Interfaces;

/// <summary>
/// Interface for caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache by key
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="key">The cache key</param>
    /// <returns>The cached value or default(T) if not found</returns>
    Task<T?> GetAsync<T>(string key);
    
    /// <summary>
    /// Sets a value in the cache
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    Task RemoveAsync(string key);
} 