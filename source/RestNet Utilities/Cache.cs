using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Caching;

namespace RestNet
{
    public static class Cache
    {
        private static System.Web.HttpContext context = System.Web.HttpContext.Current;
        private static TimeSpan defaultCacheDuration = TimeSpan.FromSeconds(int.Parse(Settings.Get("DefaultCacheDuration", "60")));
        private static TimeSpan maxCacheDuration = TimeSpan.FromDays(365); // Max cache duration is one year. See http://support.microsoft.com/kb/311388
        private static bool initialized = false;

        private static bool IsCacheEnabled
        {
            get
            {
                if (!initialized)
                {
                    try
                    {
                        System.Web.HttpRequest request = context.Request;
                        initialized = true;
                    }
                    catch (System.Web.HttpException)
                    {
                    }
                }
                return (initialized && context != null && context.Request.Params["DisableCaching"] != "yes");
            }
        }

        /// <summary>
        /// Retrieves an item from the RestNet cache
        /// </summary>
        /// <param name="key">Key to use to locate the item</param>
        /// <returns>If cache is enabled and item is located, returns it as an object, otherwise returns null</returns>
        public static object Get(string key)
        {
            try
            {
                if (!IsCacheEnabled)
                    return null;

                object cachedObject = context.Cache.Get(key);
                //Logging.DebugFormat("Cache {0} '{1}'", cachedObject == null ? "Miss  " : "Hit   ", key);
                return (cachedObject is ICloneable) ? ((ICloneable)cachedObject).Clone() : cachedObject;
            }
            catch (Exception ex)
            {
                Logging.Error(string.Format("RestNet.Cache.Get - {0} getting item '{1}'. Msg: {2}. Returning null.", ex.GetType().Name, key, ex.Message), ex);
                return null;
            }
        }

        /// <summary>
        /// Saves an item to the RestNet cache using default cache duration
        /// </summary>
        /// <param name="key">Key to use to locate the item</param>
        /// <param name="value">Object to cache</param>
        public static void Set(string key, object value)
        {
            Set(key, value, null, null, false, defaultCacheDuration);
        }

        /// <summary>
        /// Saves an item to the RestNet cache using a specified cache duration
        /// </summary>
        /// <param name="key">Key to use to locate the item</param>
        /// <param name="value">Object to cache</param>
        /// <param name="cacheDuration">Length of time to cache this object</param>
        public static void Set(string key, object value, TimeSpan cacheDuration)
        {
            Set(key, value, null, null, false, cacheDuration);
        }

        /// <summary>
        /// Saves an item to the RestNet cache using a specified cache duration
        /// </summary>
        /// <param name="key">Key to use to locate the item</param>
        /// <param name="value">Object to cache</param>
        /// <param name="allowReferenceTypes">Flag to override the prohibition on caching reference types, as they are not threadsafe</param>
        public static void Set(string key, object value, bool allowReferenceTypes)
        {
            try
            {
                if (!IsCacheEnabled)
                    return;

                if (value == null)
                {
                    if (context.Cache[key] != null)
                        context.Cache.Remove(key);
                }
                else
                {
                    Set(key, value, null, null, allowReferenceTypes, defaultCacheDuration);
                }
            }
            catch (Exception ex)
            {
                Logging.Error(string.Format("RestNet.Cache.Set - {0} setting item '{1}'. Msg: {2}", ex.GetType().Name, key, ex.Message), ex);
                return;
            }
        }

        /// <summary>
        /// Saves an item to the RestNet cache using a specified cache duration
        /// </summary>
        /// <param name="key">Key to use to locate the item</param>
        /// <param name="value">Object to cache</param>
        /// <param name="filenameDependencies">Array of absolute file paths this item is dependent on. When any of these files change, this item will expire from cache.</param>
        /// <param name="cacheDependencyKeys">Array of cache keys that this item is dependent on. When any of those items change, this item will expire from cache.</param>
        public static void Set(string key, object value, string[] filenameDependencies, string[] cacheDependencyKeys)
        {
            Set(key, value, filenameDependencies, cacheDependencyKeys, false, defaultCacheDuration);
        }

        /// <summary>
        /// Saves an item to the RestNet cache using a specified cache duration
        /// </summary>
        /// <param name="key">Key to use to locate the item</param>
        /// <param name="value">Object to cache</param>
        /// <param name="filenameDependencies">Array of absolute file paths this item is dependent on. When any of these files change, this item will expire from cache.</param>
        /// <param name="cacheDependencyKeys">Array of cache keys that this item is dependent on. When any of those items change, this item will expire from cache.</param>
        /// <param name="allowReferenceTypes">Flag to override the prohibition on caching reference types, as they are not threadsafe</param>
        /// <param name="cacheDuration">Length of time to cache this object</param>
        public static void Set(string key, object value, string[] filenameDependencies, string[] cacheDependencyKeys, bool allowReferenceTypes, TimeSpan cacheDuration)
        {
            if (context == null)
                return;

            if (value == null)
            {
                if (context.Cache[key] != null)
                    context.Cache.Remove(key);
                return;
            }

            object boxedObject = value;
            if (object.ReferenceEquals(boxedObject, value))
            {
                if (value is ICloneable)
                    boxedObject = ((ICloneable)value).Clone();
                else
                    if (!allowReferenceTypes)
                        throw new Exception(string.Format("Cannot safely cache objects of type {0}. Only value types or ICloneable reference types may be cached.", value.GetType().Name));
            }

            // TimeSpan.MaxValue is actually rejected by cache class, so use an arbitrary big value instead
            if (cacheDuration != null && cacheDuration > maxCacheDuration)
                cacheDuration = maxCacheDuration;

            // for now anyway, only cache for 1 minute. This should dramatically reduce load at peak periods
            // and still prevent stale cache entries even if we miss a cache refresh somewhere
            //Logging.DebugFormat("Cache Insert '{0}'", key);
            context.Cache.Insert(key, boxedObject, new CacheDependency(filenameDependencies, cacheDependencyKeys), System.Web.Caching.Cache.NoAbsoluteExpiration, cacheDuration, CacheItemPriority.Default, new CacheItemRemovedCallback(OnCacheItemRemoved));
        }

        /// <summary>
        /// Clears all items from the RestNet cache by modifying the master cache key
        /// </summary>
        public static void Reset()
        {
            if (context == null)
                return;
            Logging.Debug("RestNet.Cache.Reset() - Clearing all entries from cache");
            System.Collections.IDictionaryEnumerator cacheEnumerator = context.Cache.GetEnumerator();
            while(cacheEnumerator.MoveNext())
            {
                context.Cache.Remove(cacheEnumerator.Key.ToString());
            }
        }

        private static void OnCacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            if(reason == CacheItemRemovedReason.DependencyChanged)
                Logging.DebugFormat("Cached item '{0}' removed from cache. Dependent item has changed.", key);
        }
    }
}
