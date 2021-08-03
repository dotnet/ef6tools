// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Utilities
{
    using System.Collections.Generic;

    internal static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            DebugCheck.NotNull(set);
            DebugCheck.NotNull(items);

            foreach (var i in items)
            {
                set.Add(i);
            }
        }
    }
}
