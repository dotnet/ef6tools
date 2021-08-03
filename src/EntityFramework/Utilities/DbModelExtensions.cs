// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Utilities
{
    using System.Data.Entity.Infrastructure;
    using System.Xml.Linq;

    internal static class DbModelExtensions
    {
        public static XDocument GetModel(this DbModel model)
        {
            DebugCheck.NotNull(model);

            return DbContextExtensions.GetModel(w => EdmxWriter.WriteEdmx(model, w));
        }
    }
}
