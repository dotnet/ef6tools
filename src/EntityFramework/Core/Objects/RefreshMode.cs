// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Defines the different ways to handle modified properties when refreshing in-memory data from the database.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum RefreshMode
    {
        /// <summary>
        /// For unmodified client objects, same behavior as StoreWins.  For modified client
        /// objects, Refresh original values with store value, keeping all values on client
        /// object. The next time an update happens, all the client change units will be
        /// considered modified and require updating.
        /// </summary>
        ClientWins = MergeOption.PreserveChanges,

        /// <summary>
        /// Discard all changes on the client and refresh values with store values.
        /// Client original values is updated to match the store.
        /// </summary>
        StoreWins = MergeOption.OverwriteChanges,
    }
}
