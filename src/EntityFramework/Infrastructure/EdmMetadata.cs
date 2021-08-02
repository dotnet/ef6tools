﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Entity.Internal;
    using System.Data.Entity.Utilities;

    /// <summary>
    /// Represents an entity used to store metadata about an EDM in the database.
    /// </summary>
    [Obsolete(
        "EdmMetadata is no longer used. The Code First Migrations <see cref=\"EdmModelDiffer\" /> is used instead.")]
    public class EdmMetadata
    {
        #region Entity properties

        /// <summary>
        /// Gets or sets the ID of the metadata entity, which is currently always 1.
        /// </summary>
        /// <value> The id. </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the model hash which is used to check whether the model has
        /// changed since the database was created from it.
        /// </summary>
        /// <value> The model hash. </value>
        public string ModelHash { get; set; }

        #endregion

        #region Helper method for getting model hash

        /// <summary>
        /// Attempts to get the model hash calculated by Code First for the given context.
        /// This method will return null if the context is not being used in Code First mode.
        /// </summary>
        /// <param name="context"> The context. </param>
        /// <returns> The hash string. </returns>
        public static string TryGetModelHash(DbContext context)
        {
            Check.NotNull(context, "context");

            var compiledModel = context.InternalContext.CodeFirstModel;
            return compiledModel == null ? null : new ModelHashCalculator().Calculate(compiledModel);
        }

        #endregion
    }
}
