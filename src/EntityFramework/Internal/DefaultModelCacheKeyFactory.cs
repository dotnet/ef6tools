// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Utilities;

    internal sealed class DefaultModelCacheKeyFactory
    {
        public IDbModelCacheKey Create(DbContext context)
        {
            Check.NotNull(context, "context");

            string customKey = null;

            var modelCacheKeyProvider = context as IDbModelCacheKeyProvider;

            if (modelCacheKeyProvider != null)
            {
                customKey = modelCacheKeyProvider.CacheKey;
            }

            return new DefaultModelCacheKey(
                context.GetType(),
                context.InternalContext.ProviderName,
                context.InternalContext.ProviderFactory.GetType(),
                customKey);
        }
    }
}
