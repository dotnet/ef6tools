﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Migrations.History;
    using System.Data.Entity.ModelConfiguration.Edm.Serialization;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Xml;

    /// <summary>
    /// Contains methods used to access the Entity Data Model created by Code First in the EDMX form.
    /// These methods are typically used for debugging when there is a need to look at the model that
    /// Code First creates internally.
    /// </summary>
    public static class EdmxWriter
    {
        #region WriteEdmx

        /// <summary>
        /// Uses Code First with the given context and writes the resulting Entity Data Model to the given
        /// writer in EDMX form.  This method can only be used with context instances that use Code First
        /// and create the model internally.  The method cannot be used for contexts created using Database
        /// First or Model First, for contexts created using a pre-existing <see cref="ObjectContext" />, or
        /// for contexts created using a pre-existing <see cref="DbCompiledModel" />.
        /// </summary>
        /// <param name="context"> The context. </param>
        /// <param name="writer"> The writer. </param>
        public static void WriteEdmx(DbContext context, XmlWriter writer)
        {
            Check.NotNull(context, "context");
            Check.NotNull(writer, "writer");

            var internalContext = context.InternalContext;
            if (internalContext is EagerInternalContext)
            {
                throw Error.EdmxWriter_EdmxFromObjectContextNotSupported();
            }

            var modelBeingInitialized = internalContext.ModelBeingInitialized;
            if (modelBeingInitialized != null)
            {
                WriteEdmx(modelBeingInitialized, writer);
                return;
            }

            var compiledModel = internalContext.CodeFirstModel;
            if (compiledModel == null)
            {
                throw Error.EdmxWriter_EdmxFromModelFirstNotSupported();
            }

            var modelStore = DbConfiguration.DependencyResolver.GetService<DbModelStore>();
            if (modelStore != null)
            {
                var storedModel = modelStore.TryGetEdmx(context.GetType());
                if (storedModel != null)
                {
                    storedModel.WriteTo(writer);
                    return;
                }
            }

            var cachedModelBuilder = compiledModel.CachedModelBuilder;
            if (cachedModelBuilder == null)
            {
                throw Error.EdmxWriter_EdmxFromRawCompiledModelNotSupported();
            }
            
            var builder = cachedModelBuilder.Clone();

            WriteEdmx(
                internalContext.ModelProviderInfo == null
                    ? builder.Build(internalContext.Connection)
                    : builder.Build(internalContext.ModelProviderInfo),
                writer);
        }

        /// <summary>
        /// Writes the Entity Data Model represented by the given <see cref="DbModel" /> to the
        /// given writer in EDMX form.
        /// </summary>
        /// <param name="model"> An object representing the EDM. </param>
        /// <param name="writer"> The writer. </param>
        public static void WriteEdmx(DbModel model, XmlWriter writer)
        {
            Check.NotNull(model, "model");
            Check.NotNull(writer, "writer");

            new EdmxSerializer().Serialize(model.DatabaseMapping, writer);
        }

        #endregion
    }
}
