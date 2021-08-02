// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;

    // <summary>
    // Abstracts the properties of an End element in a relationship
    // </summary>
    internal interface IRelationshipEnd
    {
        // <summary>
        // Name of the End
        // </summary>
        string Name { get; }

        // <summary>
        // Type of the End
        // </summary>
        SchemaEntityType Type { get; }

        // <summary>
        // Multiplicity of the End
        // </summary>
        RelationshipMultiplicity? Multiplicity { get; set; }

        // <summary>
        // The On&lt;Operation&gt;s defined for the End
        // </summary>
        ICollection<OnOperation> Operations { get; }
    }
}
