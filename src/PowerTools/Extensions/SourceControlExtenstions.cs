// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
namespace Microsoft.DbContextPackage.Extensions
{
    using EnvDTE;
    using Microsoft.DbContextPackage.Utilities;

    internal static class SourceControlExtenstions
    {
        public static bool CheckOutItemIfNeeded(this SourceControl sourceControl, string itemName)
        {
            DebugCheck.NotNull(sourceControl);
            DebugCheck.NotEmpty(itemName);

            if (sourceControl.IsItemUnderSCC(itemName) && !sourceControl.IsItemCheckedOut(itemName))
            {
                return sourceControl.CheckOutItem(itemName);
            }

            return false;
        }
    }
}
