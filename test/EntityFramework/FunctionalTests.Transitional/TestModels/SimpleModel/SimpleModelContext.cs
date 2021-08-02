﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SimpleModel
{
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;

    [DbModelBuilderVersion(DbModelBuilderVersion.Latest)]
    public class SimpleModelContext : DbContext
    {
        public SimpleModelContext()
        {
        }

        public SimpleModelContext(DbCompiledModel model)
            : base(model)
        {
        }

        public SimpleModelContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public SimpleModelContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
        }

        public SimpleModelContext(DbConnection existingConnection, bool contextOwnsConnection = false)
            : base(existingConnection, contextOwnsConnection)
        {
        }

        public SimpleModelContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection = false)
            : base(existingConnection, model, contextOwnsConnection)
        {
        }

        public SimpleModelContext(ObjectContext objectContext, bool dbContextOwnsObjectContext = false)
            : base(objectContext, dbContextOwnsObjectContext)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        public static DbModelBuilder CreateBuilder()
        {
            var builder = new DbModelBuilder();

            builder.Entity<Product>();
            builder.Entity<Category>();

            return builder;
        }
    }
}
