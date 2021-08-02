// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Utilities;
    using System.Linq;

    public sealed class AdventureWorksModelBuilder : DbModelBuilder
    {
        private readonly Type[] _unignoredTypes;

        public AdventureWorksModelBuilder(params Type[] unignoredTypes)
        {
            _unignoredTypes = unignoredTypes;
        }

        public override DbModel Build(DbProviderInfo providerInfo)
        {
            IgnoreAll(_unignoredTypes);

            return base.Build(providerInfo);
        }

        private void IgnoreAll(params Type[] unignoredTypes)
        {
            Ignore(
                typeof(AdventureWorksModelBuilder).Assembly().GetTypes()
                    .Where(
                        t => !string.IsNullOrWhiteSpace(t.Namespace)
                             && t.Namespace.Contains("Model")).Except(
                                 Configurations.GetConfiguredTypes().Union(unignoredTypes)));
        }
    }
}
