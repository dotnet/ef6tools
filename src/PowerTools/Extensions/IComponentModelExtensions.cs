// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
namespace Microsoft.DbContextPackage.Extensions
{
    using System;
    using System.Diagnostics;
    using Microsoft.DbContextPackage.Utilities;
    using Microsoft.VisualStudio.ComponentModelHost;

    internal static class IComponentModelExtensions
    {
        public static object GetService(this IComponentModel componentModel, Type serviceType)
        {
            DebugCheck.NotNull(componentModel);
            DebugCheck.NotNull(serviceType);

            return typeof(IComponentModel).GetMethod("GetService")
                .MakeGenericMethod(serviceType)
                .Invoke(componentModel, null);
        }
    }
}
