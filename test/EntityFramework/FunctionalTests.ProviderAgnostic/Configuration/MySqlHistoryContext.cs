﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if !NET40
namespace System.Data.Entity.Configuration
{
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Migrations.History;

    public class MySqlHistoryContext : HistoryContext
    {
        public MySqlHistoryContext(
            DbConnection existingConnection,
            string defaultSchema)
            : base(existingConnection, defaultSchema)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HistoryRow>().Property(h => h.MigrationId).HasMaxLength(100).IsRequired();
            modelBuilder.Entity<HistoryRow>().Property(h => h.ContextKey).HasMaxLength(200).IsRequired();
        }
    }
}
#endif