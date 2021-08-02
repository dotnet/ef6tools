// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Entity.Utilities;

    internal class ProviderInvariantName : IProviderInvariantName
    {
        public ProviderInvariantName(string name)
        {
            DebugCheck.NotEmpty(name);

            Name = name;
        }

        public string Name { get; private set; }
    }
}
