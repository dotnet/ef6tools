// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Utilities
{
    using System.Data.Common;
    using System.Diagnostics.CodeAnalysis;

    internal class EmptyContext : DbContext
    {
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EmptyContext(DbConnection existingConnection)
            : base(existingConnection, false)
        {
            InternalContext.InitializerDisabled = true;
        }
    }
}
