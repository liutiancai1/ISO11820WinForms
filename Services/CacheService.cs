using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestServer.Models;
using ISO11820WinForms.Models;
using Serilog;

namespace ISO11820WinForms.Services
{
    /// <summary>
    /// 数据缓存服务类
    /// 性能优化：减少数据库访问，提高查询性能
    /// </summary>
    public class CacheService
    {
        private static CacheService? _instance;
        private static readonly object _lock = new object();

        // 缓存字典
        private Dictionary<string, CacheEntry<Testmaster>> _testmasterCache;
        private Dictionary<string, CacheEntry<Productmaster>> _productmasterCache;
        private Dictionary<string, CacheEntry<Apparatus>> _apparatusCache;
        private Dictionary<string, CacheEntry<Operator>> _operatorCache;

        // 缓存过期时间（分钟）
        private const int CACHE_EXPIRATION_MINUTES = 30;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static CacheService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CacheService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private CacheService()
        {
            _testmasterCache = new Dictionary<string, CacheEntry<Testmaster>>();
            _productmasterCache = new Dictionary<string, CacheEntry<Productmaster>>();
            _apparatusCache = new Dictionary<string, CacheEntry<Apparatus>>();
            _operatorCache = new Dictionary<string, CacheEntry<Operator>>();

            Log.Information("缓存服务已初始化");
        }

        #region Testmaster 缓存

        /// <summary>
        /// 获取试验数据（带缓存）
        /// </summary>
        public async Task<Testmaster?> GetTestmasterAsync(string productId, string testId)
        {
            string key = $"{productId}_{testId}";

            // 检查缓存
            if (_testmasterCache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                Log.Debug("从缓存获取试验数据: {Key}", key);
                return entry.Value;
            }

            // 从数据库查询
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    var testmaster = await context.Testmasters
                        .Include(t => t.Product)
                        .FirstOrDefaultAsync(t => t.Productid == productId && t.Testid == testId);

                    if (testmaster != null)
                    {
                        // 更新缓存
                        _testmasterCache[key] = new CacheEntry<Testmaster>(testmaster, CACHE_EXPIRATION_MINUTES);
                        Log.Debug("试验数据已缓存: {Key}", key);
                    }

                    return testmaster;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取试验数据失败: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 清除试验数据缓存
        /// </summary>
        public void ClearTestmasterCache(string? productId = null, string? testId = null)
        {
            if (productId != null && testId != null)
            {
                string key = $"{productId}_{testId}";
                _testmasterCache.Remove(key);
                Log.Debug("已清除试验数据缓存: {Key}", key);
            }
            else
            {
                _testmasterCache.Clear();
                Log.Debug("已清除所有试验数据缓存");
            }
        }

        #endregion

        #region Productmaster 缓存

        /// <summary>
        /// 获取产品数据（带缓存）
        /// </summary>
        public async Task<Productmaster?> GetProductmasterAsync(string productId)
        {
            // 检查缓存
            if (_productmasterCache.TryGetValue(productId, out var entry) && !entry.IsExpired)
            {
                Log.Debug("从缓存获取产品数据: {ProductId}", productId);
                return entry.Value;
            }

            // 从数据库查询
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    var productmaster = await context.Productmasters
                        .FirstOrDefaultAsync(p => p.Productid == productId);

                    if (productmaster != null)
                    {
                        // 更新缓存
                        _productmasterCache[productId] = new CacheEntry<Productmaster>(productmaster, CACHE_EXPIRATION_MINUTES);
                        Log.Debug("产品数据已缓存: {ProductId}", productId);
                    }

                    return productmaster;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取产品数据失败: {ProductId}", productId);
                return null;
            }
        }

        /// <summary>
        /// 清除产品数据缓存
        /// </summary>
        public void ClearProductmasterCache(string? productId = null)
        {
            if (productId != null)
            {
                _productmasterCache.Remove(productId);
                Log.Debug("已清除产品数据缓存: {ProductId}", productId);
            }
            else
            {
                _productmasterCache.Clear();
                Log.Debug("已清除所有产品数据缓存");
            }
        }

        #endregion

        #region Apparatus 缓存

        /// <summary>
        /// 获取设备数据（带缓存）
        /// </summary>
        public async Task<Apparatus?> GetApparatusAsync(int apparatusId)
        {
            string key = apparatusId.ToString();

            // 检查缓存
            if (_apparatusCache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                Log.Debug("从缓存获取设备数据: {ApparatusId}", apparatusId);
                return entry.Value;
            }

            // 从数据库查询
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    var apparatus = await context.Apparatuses
                        .FirstOrDefaultAsync(a => a.Apparatusid == apparatusId);

                    if (apparatus != null)
                    {
                        // 更新缓存
                        _apparatusCache[key] = new CacheEntry<Apparatus>(apparatus, CACHE_EXPIRATION_MINUTES);
                        Log.Debug("设备数据已缓存: {ApparatusId}", apparatusId);
                    }

                    return apparatus;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取设备数据失败: {ApparatusId}", apparatusId);
                return null;
            }
        }

        /// <summary>
        /// 清除设备数据缓存
        /// </summary>
        public void ClearApparatusCache(int? apparatusId = null)
        {
            if (apparatusId != null)
            {
                string key = apparatusId.Value.ToString();
                _apparatusCache.Remove(key);
                Log.Debug("已清除设备数据缓存: {ApparatusId}", apparatusId);
            }
            else
            {
                _apparatusCache.Clear();
                Log.Debug("已清除所有设备数据缓存");
            }
        }

        #endregion

        #region Operator 缓存

        /// <summary>
        /// 获取操作员数据（带缓存）
        /// </summary>
        public async Task<Operator?> GetOperatorAsync(string userId)
        {
            // 检查缓存
            if (_operatorCache.TryGetValue(userId, out var entry) && !entry.IsExpired)
            {
                Log.Debug("从缓存获取操作员数据: {UserId}", userId);
                return entry.Value;
            }

            // 从数据库查询
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    var operatorData = await context.Operators
                        .FirstOrDefaultAsync(o => o.Userid == userId);

                    if (operatorData != null)
                    {
                        // 更新缓存
                        _operatorCache[userId] = new CacheEntry<Operator>(operatorData, CACHE_EXPIRATION_MINUTES);
                        Log.Debug("操作员数据已缓存: {UserId}", userId);
                    }

                    return operatorData;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取操作员数据失败: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// 清除操作员数据缓存
        /// </summary>
        public void ClearOperatorCache(string? userId = null)
        {
            if (userId != null)
            {
                _operatorCache.Remove(userId);
                Log.Debug("已清除操作员数据缓存: {UserId}", userId);
            }
            else
            {
                _operatorCache.Clear();
                Log.Debug("已清除所有操作员数据缓存");
            }
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            _testmasterCache.Clear();
            _productmasterCache.Clear();
            _apparatusCache.Clear();
            _operatorCache.Clear();

            Log.Information("已清除所有缓存");
        }

        /// <summary>
        /// 清除过期缓存
        /// </summary>
        public void ClearExpiredCache()
        {
            int expiredCount = 0;

            // 清除过期的试验数据缓存
            var expiredTestmasters = _testmasterCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredTestmasters)
            {
                _testmasterCache.Remove(key);
                expiredCount++;
            }

            // 清除过期的产品数据缓存
            var expiredProducts = _productmasterCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredProducts)
            {
                _productmasterCache.Remove(key);
                expiredCount++;
            }

            // 清除过期的设备数据缓存
            var expiredApparatuses = _apparatusCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredApparatuses)
            {
                _apparatusCache.Remove(key);
                expiredCount++;
            }

            // 清除过期的操作员数据缓存
            var expiredOperators = _operatorCache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredOperators)
            {
                _operatorCache.Remove(key);
                expiredCount++;
            }

            if (expiredCount > 0)
            {
                Log.Debug("已清除 {Count} 个过期缓存项", expiredCount);
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int testmasterCount, int productmasterCount, int apparatusCount, int operatorCount) GetCacheStats()
        {
            return (
                _testmasterCache.Count,
                _productmasterCache.Count,
                _apparatusCache.Count,
                _operatorCache.Count
            );
        }

        #endregion

        /// <summary>
        /// 缓存条目类
        /// </summary>
        private class CacheEntry<T>
        {
            public T Value { get; }
            public DateTime ExpirationTime { get; }

            public bool IsExpired => DateTime.Now > ExpirationTime;

            public CacheEntry(T value, int expirationMinutes)
            {
                Value = value;
                ExpirationTime = DateTime.Now.AddMinutes(expirationMinutes);
            }
        }
    }
}
