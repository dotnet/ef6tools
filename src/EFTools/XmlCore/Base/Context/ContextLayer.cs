﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Base.Context
{
    using System;

    /// <summary>
    ///     The items in the context item manager are divided into layers.
    ///     A layer may be isolated, in which it does not inherit context
    ///     from previous layers, or normal, in which it does.  Once a layer
    ///     is created new context items can be set into that layer.  When
    ///     the layer is removed, all the prior context items come back.
    /// </summary>
    internal abstract class ContextLayer : IDisposable
    {
        /// <summary>
        ///     Implements the finalization part of the IDisposable pattern by calling
        ///     Dispose(false).
        /// </summary>
        ~ContextLayer()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Removes this layer from the items.
        /// </summary>
        internal abstract void Remove();

        /// <summary>
        ///     Disposes this layer by calling Dispose(true).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Disposes this layer by calling Remove on it.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Remove();
            }
        }
    }
}
