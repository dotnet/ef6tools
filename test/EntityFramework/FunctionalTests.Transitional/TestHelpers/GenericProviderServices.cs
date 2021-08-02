// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;

    public class GenericProviderServices : DbProviderServices
    {
        public static GenericProviderServices Instance = new GenericProviderServices();

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            throw new NotImplementedException();
        }

        protected override string GetDbProviderManifestToken(Common.DbConnection connection)
        {
            throw new NotImplementedException();
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            throw new NotImplementedException();
        }
    }
}
