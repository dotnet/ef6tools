// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Pluralization;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.Utilities;
    using System.Linq;

    /// <summary>
    /// Convention to set the table name to be a pluralized version of the entity type name.
    /// </summary>
    public class PluralizingTableNameConvention : IStoreModelConvention<EntityType>
    {
        private IPluralizationService _pluralizationService
            = DbConfiguration.DependencyResolver.GetService<IPluralizationService>();

        /// <inheritdoc />
        public virtual void Apply(EntityType item, DbModel model)
        {
            Check.NotNull(item, "item");
            Check.NotNull(model, "model");

            _pluralizationService = DbConfiguration.DependencyResolver.GetService<IPluralizationService>();

            if (item.GetTableName() == null)
            {
                var entitySet = model.StoreModel.GetEntitySet(item);

                entitySet.Table
                    = model.StoreModel.GetEntitySets()
                        .Where(es => es.Schema == entitySet.Schema)
                        .Except(new[] { entitySet })
                        .Select(n => n.Table)
                        .Uniquify(_pluralizationService.Pluralize(entitySet.Table));
            }
        }
    }
}
