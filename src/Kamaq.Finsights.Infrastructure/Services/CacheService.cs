using Kamaq.Finsights.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Kamaq.Finsights.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        _logger.LogDebug("Getting value for key {Key} from cache", key);
        
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache miss for key {Key}", key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        _logger.LogDebug("Setting value for key {Key} in cache with expiration {Expiration}", 
            key, expiration?.ToString() ?? "default");
            
        _memoryCache.Set(key, value, options);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _logger.LogDebug("Removing key {Key} from cache", key);
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
} 