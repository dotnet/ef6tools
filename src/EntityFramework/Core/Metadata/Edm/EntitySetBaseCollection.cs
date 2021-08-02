// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;

    // <summary>
    // Class representing a collection of entity set objects
    // </summary>
    internal sealed class EntitySetBaseCollection : MetadataCollection<EntitySetBase>
    {
        // This collection allows changes to be intercepted before and after they are passed to MetadataCollection.  The interception
        // is required to update the EntitySet's back-reference to the EntityContainer.

        // <summary>
        // Default constructor for constructing an empty collection
        // </summary>
        // <param name="entityContainer"> The entity container that has this entity set collection </param>
        // <exception cref="System.ArgumentNullException">Thrown if the argument entityContainer is null</exception>
        internal EntitySetBaseCollection(EntityContainer entityContainer)
            : this(entityContainer, null)
        {
        }

        // <summary>
        // The constructor for constructing the collection with the given items
        // </summary>
        // <param name="entityContainer"> The entity container that has this entity set collection </param>
        // <param name="items"> The items to populate the collection </param>
        // <exception cref="System.ArgumentNullException">Thrown if the argument entityContainer is null</exception>
        internal EntitySetBaseCollection(EntityContainer entityContainer, IEnumerable<EntitySetBase> items)
            : base(items)
        {
            Check.NotNull(entityContainer, "entityContainer");
            _entityContainer = entityContainer;
        }

        private readonly EntityContainer _entityContainer;

        // <summary>
        // Gets an item from the collection with the given index
        // </summary>
        // <param name="index"> The index to search for </param>
        // <returns> An item from the collection </returns>
        // <exception cref="System.ArgumentOutOfRangeException">Thrown if the index is out of the range for the Collection</exception>
        // <exception cref="System.InvalidOperationException">Always thrown on setter</exception>
        public override EntitySetBase this[int index]
        {
            get { return base[index]; }
            set { throw new InvalidOperationException(Strings.OperationOnReadOnlyCollection); }
        }

        // <summary>
        // Gets an item from the collection with the given identity
        // </summary>
        // <param name="identity"> The identity of the item to search for </param>
        // <returns> An item from the collection </returns>
        // <exception cref="System.ArgumentNullException">Thrown if identity argument passed in is null</exception>
        // <exception cref="System.ArgumentException">Thrown if the Collection does not have an EntitySet with the given identity</exception>
        // <exception cref="System.InvalidOperationException">Always thrown on setter</exception>
        public override EntitySetBase this[string identity]
        {
            get { return base[identity]; }
            set { throw new InvalidOperationException(Strings.OperationOnReadOnlyCollection); }
        }

        // <summary>
        // Adds an item to the collection
        // </summary>
        // <param name="item"> The item to add to the list </param>
        // <exception cref="System.ArgumentNullException">Thrown if item argument is null</exception>
        // <exception cref="System.InvalidOperationException">Thrown if the item passed in or the collection itself instance is in ReadOnly state</exception>
        // <exception cref="System.ArgumentException">Thrown if the EntitySetBase that is being added already belongs to another EntityContainer</exception>
        // <exception cref="System.ArgumentException">Thrown if the EntitySetCollection already contains an EntitySet with the same identity</exception>
        public override void Add(EntitySetBase item)
        {
            Check.NotNull(item, "item");
            // Check to make sure the given entity set is not associated with another type
            ThrowIfItHasEntityContainer(item, "item");
            base.Add(item);

            // Fix up the declaring type
            item.ChangeEntityContainerWithoutCollectionFixup(_entityContainer);
        }

        // <summary>
        // Checks if the given entity set already has a entity container, if so, throw an exception
        // </summary>
        // <param name="entitySet"> The entity set to check for </param>
        // <param name="argumentName"> The name of the argument from the caller </param>
        private static void ThrowIfItHasEntityContainer(EntitySetBase entitySet, string argumentName)
        {
            Check.NotNull(entitySet, argumentName);
            if (entitySet.EntityContainer != null)
            {
                throw new ArgumentException(Strings.EntitySetInAnotherContainer, argumentName);
            }
        }
    }
}
