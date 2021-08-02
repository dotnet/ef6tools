// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.EntitySql
{
    using System.Collections.Generic;

    // <summary>
    // Represents a pair of types to avoid uncessary enumerations to split kvp elements
    // </summary>
    internal sealed class Pair<L, R>
    {
        internal Pair(L left, R right)
        {
            Left = left;
            Right = right;
        }

        internal L Left;
        internal R Right;

        internal KeyValuePair<L, R> GetKVP()
        {
            return new KeyValuePair<L, R>(Left, Right);
        }
    }
}
