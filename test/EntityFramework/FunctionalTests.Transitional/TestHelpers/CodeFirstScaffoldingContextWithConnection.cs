// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestHelpers
{
    using SimpleModel;

    public class CodeFirstScaffoldingContextWithConnection : DbContext
    {
        public string ExtraInfo { get; private set; }

        public CodeFirstScaffoldingContextWithConnection(string extraInfo)
            : base("name=CodeFirstConnectionString")
        {
            ExtraInfo = extraInfo;
        }

        public DbSet<Product> Products { get; set; }
    }
}
