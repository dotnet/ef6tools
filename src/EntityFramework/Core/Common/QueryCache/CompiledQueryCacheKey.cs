// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.QueryCache
{
    using System.Data.Entity.Utilities;

    internal sealed class CompiledQueryCacheKey : QueryCacheKey
    {
        private readonly Guid _cacheIdentity;

        internal CompiledQueryCacheKey(Guid cacheIdentity)
        {
            _cacheIdentity = cacheIdentity;
        }

        // <summary>
        // Determines equality of this key with respect to <paramref name="compareTo" />
        // </summary>
        public override bool Equals(object compareTo)
        {
            DebugCheck.NotNull(compareTo);
            if (typeof(CompiledQueryCacheKey) != compareTo.GetType())
            {
                return false;
            }

            return ((CompiledQueryCacheKey)compareTo)._cacheIdentity.Equals(_cacheIdentity);
        }

        // <summary>
        // Returns the hashcode for this cache key
        // </summary>
        public override int GetHashCode()
        {
            return _cacheIdentity.GetHashCode();
        }

        // <summary>
        // Returns a string representation of the state of this cache key
        // </summary>
        // <returns> A string representation that includes query text, parameter information, include path information and merge option information about this cache key. </returns>
        public override string ToString()
        {
            return _cacheIdentity.ToString();
        }
    }
}
