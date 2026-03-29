using ASC.Web.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.IO;

namespace ASC.Web.Data
{
    public class NavigationCacheOperations : INavigationCacheOperations
    {
        private readonly IDistributedCache _cache;
        private readonly string NavigationCacheName = "NavigationCache";

        public NavigationCacheOperations(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task CreateNavigationCacheAsync()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Navigation", "Navigation.json");
            await _cache.SetStringAsync(NavigationCacheName, File.ReadAllText(path));
        }

        public async Task<NavigationMenu> GetNavigationCacheAsync()
        {
            var json = await _cache.GetStringAsync(NavigationCacheName);
            return JsonConvert.DeserializeObject<NavigationMenu>(json!) ?? new NavigationMenu();
        }
    }
}