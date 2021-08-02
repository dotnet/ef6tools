// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.Internal
{
    // <summary>
    // A strategy interface that defines methods used for setting and getting EntityKey values on an entity.
    // Implementors of this interface are used by the EntityWrapper class.
    // </summary>
    internal interface IEntityKeyStrategy
    {
        // <summary>
        // Gets the entity key.
        // </summary>
        // <returns> The key </returns>
        EntityKey GetEntityKey();

        // <summary>
        // Sets the entity key
        // </summary>
        // <param name="key"> The key </param>
        void SetEntityKey(EntityKey key);

        // <summary>
        // Returns the entity key directly from the entity
        // </summary>
        // <returns> the key </returns>
        EntityKey GetEntityKeyFromEntity();
    }
}
