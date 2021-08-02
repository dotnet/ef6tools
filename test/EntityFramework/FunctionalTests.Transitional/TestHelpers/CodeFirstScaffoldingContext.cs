// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestHelpers
{
    using SimpleModel;

    public class CodeFirstScaffoldingContext : DbContext
    {
        public string ExtraInfo { get; private set; }

        public CodeFirstScaffoldingContext(string extraInfo)
        {
            ExtraInfo = extraInfo;
        }

        public DbSet<Product> Products { get; set; }
    }
}
