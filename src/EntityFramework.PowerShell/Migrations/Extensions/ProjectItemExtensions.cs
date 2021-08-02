// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Extensions
{
    using System.Data.Entity.Utilities;
    using EnvDTE;

    // <summary>
    // Extension methods for the Visual Studio ProjectItem interface.
    // </summary>
    internal static class ProjectItemExtensions
    {
        // <summary>
        // Returns true if the project item is named either "app.config" or "web.config".
        // </summary>
        public static bool IsConfig(this ProjectItem item)
        {
            DebugCheck.NotNull(item);

            return IsNamed(item, "app.config") || IsNamed(item, "web.config");
        }

        // <summary>
        // Returns true if the project item has the given name, with case ignored.
        // </summary>
        public static bool IsNamed(this ProjectItem item, string name)
        {
            DebugCheck.NotNull(item);

            return item.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
