// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Utilities
{
    using System.Data.Entity.Infrastructure;

    internal static class DbProviderInfoExtensions
    {
        public static bool IsSqlCe(this DbProviderInfo providerInfo)
        {
            DebugCheck.NotNull(providerInfo);

            return !string.IsNullOrWhiteSpace(providerInfo.ProviderInvariantName) &&
                   providerInfo.ProviderInvariantName.StartsWith(
                       "System.Data.SqlServerCe", StringComparison.OrdinalIgnoreCase);
        }
    }
}
