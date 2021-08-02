// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Reflection;

    internal class OcAssemblyCache
    {
        // <summary>
        // cache for loaded assembly
        // </summary>
        private readonly Dictionary<Assembly, ImmutableAssemblyCacheEntry> _conventionalOcCache;

        internal OcAssemblyCache()
        {
            _conventionalOcCache = new Dictionary<Assembly, ImmutableAssemblyCacheEntry>();
        }

        // <summary>
        // Please do NOT call this method outside of AssemblyCache. Since AssemblyCache maintain the lock,
        // this method doesn't provide any locking mechanism.
        // </summary>
        internal bool TryGetConventionalOcCacheFromAssemblyCache(Assembly assemblyToLookup, out ImmutableAssemblyCacheEntry cacheEntry)
        {
            cacheEntry = null;
            return _conventionalOcCache.TryGetValue(assemblyToLookup, out cacheEntry);
        }

        // <summary>
        // Please do NOT call this method outside of AssemblyCache. Since AssemblyCache maintain the lock,
        // this method doesn't provide any locking mechanism.
        // </summary>
        internal void AddAssemblyToOcCacheFromAssemblyCache(Assembly assembly, ImmutableAssemblyCacheEntry cacheEntry)
        {
            if (_conventionalOcCache.ContainsKey(assembly))
            {
                // we shouldn't update the cache if we already have one
                return;
            }
            _conventionalOcCache.Add(assembly, cacheEntry);
        }
    }
}
