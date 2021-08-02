﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SimpleModel
{
    using System;
    using System.Data.Common;
    using System.Data.Entity;

    public class EmptyContext : DbContext
    {
        public EmptyContext()
        {
        }

        public EmptyContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public EmptyContext(DbConnection connection, bool contextOwnsConnection = false)
            : base(connection, contextOwnsConnection)
        {
        }

        public Action<DbModelBuilder> CustomOnModelCreating { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (CustomOnModelCreating != null)
            {
                CustomOnModelCreating(modelBuilder);
            }
        }
    }
}
