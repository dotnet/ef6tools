// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    // <summary>
    // Which data model to target
    // </summary>
    internal enum SchemaDataModelOption
    {
        // <summary>
        // Target the CDM data model
        // </summary>
        EntityDataModel = 0,

        // <summary>
        // Target the data providers - SQL, Oracle, etc
        // </summary>
        ProviderDataModel = 1,

        // <summary>
        // Target the data providers - SQL, Oracle, etc
        // </summary>
        ProviderManifestModel = 2,
    }
}
