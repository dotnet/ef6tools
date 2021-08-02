// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityClient.Internal
{
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    // <summary>
    // The class for provider services of the entity client
    // </summary>
    internal sealed class EntityProviderServices : DbProviderServices
    {
        // <summary>
        // Singleton object
        // </summary>
        internal static readonly EntityProviderServices Instance = new EntityProviderServices();

        // <summary>
        // Create a Command Definition object, given the connection and command tree
        // </summary>
        // <param name="commandTree"> command tree for the statement </param>
        // <returns> an executable command definition object </returns>
        // <exception cref="ArgumentNullException">connection and commandTree arguments must not be null</exception>
        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            Check.NotNull(providerManifest, "providerManifest");
            Check.NotNull(commandTree, "commandTree");

            return CreateDbCommandDefinition(providerManifest, commandTree, new DbInterceptionContext());
        }

        internal static EntityCommandDefinition CreateCommandDefinition(
            DbProviderFactory storeProviderFactory,
            DbCommandTree commandTree,
            DbInterceptionContext interceptionContext,
            IDbDependencyResolver resolver = null)
        {
            DebugCheck.NotNull(storeProviderFactory);
            DebugCheck.NotNull(interceptionContext);
            DebugCheck.NotNull(commandTree);

            return new EntityCommandDefinition(storeProviderFactory, commandTree, interceptionContext, resolver);
        }

        internal override DbCommandDefinition CreateDbCommandDefinition(
            DbProviderManifest providerManifest,
            DbCommandTree commandTree,
            DbInterceptionContext interceptionContext)
        {
            DebugCheck.NotNull(providerManifest);
            DebugCheck.NotNull(commandTree);
            DebugCheck.NotNull(interceptionContext);

            var storeMetadata = (StoreItemCollection)commandTree.MetadataWorkspace.GetItemCollection(DataSpace.SSpace);
            return CreateCommandDefinition(storeMetadata.ProviderFactory, commandTree, interceptionContext);
        }

        // <summary>
        // Ensures that the data space of the specified command tree is the model (C-) space
        // </summary>
        // <param name="commandTree"> The command tree for which the data space should be validated </param>
        internal override void ValidateDataSpace(DbCommandTree commandTree)
        {
            DebugCheck.NotNull(commandTree);

            if (commandTree.DataSpace != DataSpace.CSpace)
            {
                throw new ProviderIncompatibleException(Strings.EntityClient_RequiresNonStoreCommandTree);
            }
        }

        // <summary>
        // Create a EntityCommandDefinition object based on the prototype command
        // This method is intended for provider writers to build a default command definition
        // from a command.
        // </summary>
        // <exception cref="ArgumentNullException">prototype argument must not be null</exception>
        // <exception cref="InvalidCastException">prototype argument must be a EntityCommand</exception>
        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            Check.NotNull(prototype, "prototype");

            return ((EntityCommand)prototype).GetCommandDefinition();
        }

        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            Check.NotNull(connection, "connection");

            if (connection.GetType() != typeof(EntityConnection))
            {
                throw new ArgumentException(Strings.Mapping_Provider_WrongConnectionType(typeof(EntityConnection)));
            }

            return MetadataItem.EdmProviderManifest.Token;
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            Check.NotNull(manifestToken, "manifestToken");

            return MetadataItem.EdmProviderManifest;
        }
    }
}
