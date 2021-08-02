// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.ComponentModel;
    using System.Data.Entity.ModelConfiguration.Configuration.Properties.Navigation;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Configures a many:many relationship.
    /// This configuration functionality is available via the Code First Fluent API, see <see cref="DbModelBuilder" />.
    /// </summary>
    /// <typeparam name="TEntityType">The type of the parent entity of the navigation property specified in the HasMany call.</typeparam>
    /// <typeparam name="TTargetEntityType">The type of the parent entity of the navigation property specified in the WithMany call.</typeparam>
    public class ManyToManyNavigationPropertyConfiguration<TEntityType, TTargetEntityType>
        where TEntityType : class
        where TTargetEntityType : class
    {
        private readonly NavigationPropertyConfiguration _navigationPropertyConfiguration;

        internal ManyToManyNavigationPropertyConfiguration(
            NavigationPropertyConfiguration navigationPropertyConfiguration)
        {
            DebugCheck.NotNull(navigationPropertyConfiguration);

            _navigationPropertyConfiguration = navigationPropertyConfiguration;
        }

        /// <summary>
        /// Configures the foreign key column(s) and table used to store the relationship.
        /// </summary>
        /// <param name="configurationAction"> Action that configures the foreign key column(s) and table. </param>
        /// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.ManyToManyNavigationPropertyConfiguration`2" /> instance so that multiple calls can be chained.</returns>
        public ManyToManyNavigationPropertyConfiguration<TEntityType, TTargetEntityType> Map(
            Action<ManyToManyAssociationMappingConfiguration> configurationAction)
        {
            Check.NotNull(configurationAction, "configurationAction");

            var manyToManyMappingConfiguration = new ManyToManyAssociationMappingConfiguration();

            configurationAction(manyToManyMappingConfiguration);

            _navigationPropertyConfiguration.AssociationMappingConfiguration = manyToManyMappingConfiguration;

            return this;
        }

        /// <summary>
        /// Configures stored procedures to be used for modifying this relationship.
        /// The default conventions for procedure and parameter names will be used.
        /// </summary>
        /// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.ManyToManyNavigationPropertyConfiguration`2" /> instance so that multiple calls can be chained.</returns>
        public ManyToManyNavigationPropertyConfiguration<TEntityType, TTargetEntityType> MapToStoredProcedures()
        {
            if (_navigationPropertyConfiguration.ModificationStoredProceduresConfiguration == null)
            {
                _navigationPropertyConfiguration.ModificationStoredProceduresConfiguration
                    = new ModificationStoredProceduresConfiguration();
            }

            return this;
        }

        /// <summary> 
        /// Configures stored procedures to be used for modifying this relationship. 
        /// </summary>
        /// <param name="modificationStoredProcedureMappingConfigurationAction">
        /// Configuration to override the default conventions for procedure and parameter names.
        /// </param>
        /// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.ManyToManyNavigationPropertyConfiguration`2" /> instance so that multiple calls can be chained.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ManyToManyNavigationPropertyConfiguration<TEntityType, TTargetEntityType> MapToStoredProcedures(
            Action<ManyToManyModificationStoredProceduresConfiguration<TEntityType, TTargetEntityType>>
                modificationStoredProcedureMappingConfigurationAction)
        {
            Check.NotNull(modificationStoredProcedureMappingConfigurationAction, "modificationStoredProcedureMappingConfigurationAction");

            var modificationStoredProcedureMappingConfiguration
                = new ManyToManyModificationStoredProceduresConfiguration<TEntityType, TTargetEntityType>();

            modificationStoredProcedureMappingConfigurationAction(modificationStoredProcedureMappingConfiguration);

            if (_navigationPropertyConfiguration.ModificationStoredProceduresConfiguration == null)
            {
                _navigationPropertyConfiguration.ModificationStoredProceduresConfiguration
                    = modificationStoredProcedureMappingConfiguration.Configuration;
            }
            else
            {
                _navigationPropertyConfiguration.ModificationStoredProceduresConfiguration
                    .Merge(modificationStoredProcedureMappingConfiguration.Configuration, allowOverride: true);
            }

            return this;
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Gets the <see cref="Type" /> of the current instance.
        /// </summary>
        /// <returns>The exact runtime type of the current instance.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
