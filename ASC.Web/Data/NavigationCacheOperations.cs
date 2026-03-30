using System.Text.Json;
using ASC.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ASC.Web.Data
{
    public class NavigationCacheOperations : INavigationCacheOperations
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IWebHostEnvironment _environment;

        private const string NavigationCacheKey = "NavigationMenu";

        public NavigationCacheOperations(
            IMemoryCache memoryCache,
            IWebHostEnvironment environment)
        {
            _memoryCache = memoryCache;
            _environment = environment;
        }

        public async Task CreateNavigationCacheAsync()
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Navigation", "Navigation.json");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy file Navigation.json", filePath);

            var json = await File.ReadAllTextAsync(filePath);

            var navigationMenu = JsonSerializer.Deserialize<NavigationMenu>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (navigationMenu == null)
                throw new Exception("Không đọc được dữ liệu Navigation.json");

            _memoryCache.Set(NavigationCacheKey, navigationMenu);
        }

        public async Task<NavigationMenu> GetNavigationCacheAsync()
        {
            if (_memoryCache.TryGetValue(NavigationCacheKey, out NavigationMenu? navigationMenu)
                && navigationMenu != null)
            {
                return navigationMenu;
            }

            await CreateNavigationCacheAsync();

            navigationMenu = _memoryCache.Get<NavigationMenu>(NavigationCacheKey);

            if (navigationMenu == null)
                throw new Exception("Không lấy được dữ liệu navigation từ cache");

            return navigationMenu;
        }
    }
}