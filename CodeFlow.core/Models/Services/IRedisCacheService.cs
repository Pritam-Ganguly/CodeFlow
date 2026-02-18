
namespace CodeFlow.core.Models.Services
{
    public interface IRedisCacheService
    {
        Task<bool> ExistsAsync(string key);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    }
}