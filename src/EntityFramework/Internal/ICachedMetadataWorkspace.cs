// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Reflection;

    // <summary>
    // Represents an object that holds a cached copy of a MetadataWorkspace and optionally the
    // assemblies containing entity types to use with that workspace.
    // </summary>
    internal interface ICachedMetadataWorkspace
    {
        // <summary>
        // Gets the MetadataWorkspace, potentially lazily creating it if it does not already exist.
        // If the workspace is not compatible with the provider manifest obtained from the given
        // connection then an exception is thrown.
        // </summary>
        // <param name="storeConnection"> The connection to use to create or check SSDL provider info. </param>
        // <returns> The workspace. </returns>
        MetadataWorkspace GetMetadataWorkspace(DbConnection storeConnection);

        // <summary>
        // The list of assemblies that contain entity types for this workspace, which may be empty, but
        // will never be null.
        // </summary>
        IEnumerable<Assembly> Assemblies { get; }

        // <summary>
        // The default container name for code first is the container name that is set from the DbModelBuilder
        // </summary>
        string DefaultContainerName { get; }

        // <summary>
        // The provider info used to construct the workspace.
        // </summary>
        DbProviderInfo ProviderInfo { get; }
    }
}
