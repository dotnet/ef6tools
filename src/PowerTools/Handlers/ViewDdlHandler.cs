﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
namespace Microsoft.DbContextPackage.Handlers
{
    using System;
    using System.IO;
    using Microsoft.DbContextPackage.Resources;
    using Microsoft.DbContextPackage.Utilities;

    internal class ViewDdlHandler
    {
        private readonly DbContextPackage _package;

        public ViewDdlHandler(DbContextPackage package)
        {
            DebugCheck.NotNull(package);

            _package = package;
        }

        public void ViewDdl(dynamic context)
        {
            Type contextType = context.GetType();

            try
            {
                var filePath = Path.Combine(
                    Path.GetTempPath(),
                    contextType.Name + FileExtensions.Sql);

                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }

                var objectContext = DbContextPackage.GetObjectContext(context);

                File.WriteAllText(filePath, objectContext.CreateDatabaseScript());
                File.SetAttributes(filePath, FileAttributes.ReadOnly);

                _package.DTE2.ItemOperations.OpenFile(filePath);
            }
            catch (Exception exception)
            {
                _package.LogError(Strings.ViewDdlError(contextType.Name), exception);
            }
        }
    }
}