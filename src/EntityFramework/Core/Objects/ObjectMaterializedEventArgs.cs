// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// EventArgs for the ObjectMaterialized event.
    /// </summary>
    public class ObjectMaterializedEventArgs : EventArgs
    {
        // <summary>
        // The object that was materialized.
        // </summary>
        private readonly object _entity;

        // <summary>
        // Constructs new arguments for the ObjectMaterialized event.
        // </summary>
        // <param name="entity"> The object that has been materialized. </param>
        internal ObjectMaterializedEventArgs(object entity)
        {
            _entity = entity;
        }

        /// <summary>Gets the entity object that was created.</summary>
        /// <returns>The entity object that was created.</returns>
        public object Entity
        {
            get { return _entity; }
        }
    }

    /// <summary>
    /// Delegate for the ObjectMaterialized event.
    /// </summary>
    /// <param name="sender"> The ObjectContext responsable for materializing the object. </param>
    /// <param name="e"> EventArgs containing a reference to the materialized object. </param>
    [SuppressMessage("Microsoft.Design", "CA1003:UseGenericEventHandlerInstances")]
    public delegate void ObjectMaterializedEventHandler(object sender, ObjectMaterializedEventArgs e);
}
