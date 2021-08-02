﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "System.Data.Entity.Migrations.Sql")]

#if SQLSERVERCOMPACT35
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "System.Data.Entity.SqlServerCompact.Legacy")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "subclause", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Legacy.Properties.Resources.SqlServerCompact.resources")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "rowversion", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Legacy.Properties.Resources.SqlServerCompact.resources")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "schemaname", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Legacy.Properties.Resources.SqlServerCompact.resources")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "objectname", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Legacy.Properties.Resources.SqlServerCompact.resources")]
#else
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "System.Data.Entity.SqlServerCompact")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "subclause", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Properties.Resources.SqlServerCompact.resources")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "rowversion", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Properties.Resources.SqlServerCompact.resources")]
[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "schemaname", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Properties.Resources.SqlServerCompact.resources")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "objectname", Scope = "resource",
        Target = "System.Data.Entity.SqlServerCompact.Properties.Resources.SqlServerCompact.resources")]
#endif