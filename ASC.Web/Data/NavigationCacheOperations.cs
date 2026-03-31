<<<<<<< HEAD
﻿using System.Text.Json;
using ASC.Web.Models;
using Microsoft.Extensions.Caching.Memory;
=======
﻿using ASC.Web.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.IO;
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90

namespace ASC.Web.Data
{
    public class NavigationCacheOperations : INavigationCacheOperations
    {
<<<<<<< HEAD
        private readonly IMemoryCache _memoryCache;
        private readonly IWebHostEnvironment _environment;

        private const string NavigationCacheKey = "NavigationMenu";

        public NavigationCacheOperations(
            IMemoryCache memoryCache,
            IWebHostEnvironment environment)
        {
            _memoryCache = memoryCache;
            _environment = environment;
=======
        private readonly IDistributedCache _cache;
        private readonly string NavigationCacheName = "NavigationCache";

        public NavigationCacheOperations(IDistributedCache cache)
        {
            _cache = cache;
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
        }

        public async Task CreateNavigationCacheAsync()
        {
<<<<<<< HEAD
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
=======
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Navigation", "Navigation.json");
            await _cache.SetStringAsync(NavigationCacheName, File.ReadAllText(path));
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
        }

        public async Task<NavigationMenu> GetNavigationCacheAsync()
        {
<<<<<<< HEAD
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
=======
            var json = await _cache.GetStringAsync(NavigationCacheName);
            return JsonConvert.DeserializeObject<NavigationMenu>(json!) ?? new NavigationMenu();
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
        }
    }
}