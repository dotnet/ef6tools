﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;

    internal class LockedAssemblyCache : IDisposable
    {
        private object _lockObject;
        private Dictionary<Assembly, ImmutableAssemblyCacheEntry> _globalAssemblyCache;

        internal LockedAssemblyCache(object lockObject, Dictionary<Assembly, ImmutableAssemblyCacheEntry> globalAssemblyCache)
        {
            _lockObject = lockObject;
            _globalAssemblyCache = globalAssemblyCache;
            Monitor.Enter(_lockObject);
        }

        public void Dispose()
        {
            // Technically, calling GC.SuppressFinalize is not required because the class does not
            // have a finalizer, but it does no harm, protects against the case where a finalizer is added
            // in the future, and prevents an FxCop warning.
            GC.SuppressFinalize(this);
            Monitor.Exit(_lockObject);
            _lockObject = null;
            _globalAssemblyCache = null;
        }

        [Conditional("DEBUG")]
        private void AssertLockedByThisThread()
        {
            var entered = false;
            Monitor.TryEnter(_lockObject, ref entered);
            if (entered)
            {
                Monitor.Exit(_lockObject);
            }

            Debug.Assert(entered, "The cache is being accessed by a thread that isn't holding the lock");
        }

        internal bool TryGetValue(Assembly assembly, out ImmutableAssemblyCacheEntry cacheEntry)
        {
            AssertLockedByThisThread();
            return _globalAssemblyCache.TryGetValue(assembly, out cacheEntry);
        }

        internal void Add(Assembly assembly, ImmutableAssemblyCacheEntry assemblyCacheEntry)
        {
            AssertLockedByThisThread();
            _globalAssemblyCache.Add(assembly, assemblyCacheEntry);
        }

        internal void Clear()
        {
            AssertLockedByThisThread();
            _globalAssemblyCache.Clear();
        }
    }
}
