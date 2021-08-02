// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm.Services
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Mapping.Update.Internal;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    internal class ModificationFunctionMappingGenerator : StructuralTypeMappingGenerator
    {
        public ModificationFunctionMappingGenerator(DbProviderManifest providerManifest)
            : base(providerManifest)
        {
        }

        public void Generate(EntityType entityType, DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(entityType);
            DebugCheck.NotNull(databaseMapping);

            if (entityType.Abstract)
            {
                return;
            }

            var entitySet = databaseMapping.Model.GetEntitySet(entityType);

            Debug.Assert(entitySet != null);

            var entitySetMapping = databaseMapping.GetEntitySetMapping(entitySet);

            Debug.Assert(entitySetMapping != null);

            var columnMappings = GetColumnMappings(entityType, entitySetMapping).ToList();
            var iaFkProperties = GetIndependentFkColumns(entityType, databaseMapping).ToList();

            var insertFunctionMapping
                = GenerateFunctionMapping(
                    ModificationOperator.Insert,
                    entitySetMapping.EntitySet,
                    entityType,
                    databaseMapping,
                    entityType.Properties,
                    iaFkProperties,
                    columnMappings,
                    entityType
                        .Properties
                        .Where(p => p.HasStoreGeneratedPattern()));

            var updateFunctionMapping
                = GenerateFunctionMapping(
                    ModificationOperator.Update,
                    entitySetMapping.EntitySet,
                    entityType,
                    databaseMapping,
                    entityType.Properties,
                    iaFkProperties,
                    columnMappings,
                    entityType
                        .Properties
                        .Where(p => p.GetStoreGeneratedPattern() == StoreGeneratedPattern.Computed));

            var deleteFunctionMapping
                = GenerateFunctionMapping(
                    ModificationOperator.Delete,
                    entitySetMapping.EntitySet,
                    entityType,
                    databaseMapping,
                    entityType.Properties,
                    iaFkProperties,
                    columnMappings);

            var modificationStoredProcedureMapping
                = new EntityTypeModificationFunctionMapping(
                    entityType,
                    deleteFunctionMapping,
                    insertFunctionMapping,
                    updateFunctionMapping);

            entitySetMapping.AddModificationFunctionMapping(modificationStoredProcedureMapping);
        }

        private static IEnumerable<ColumnMappingBuilder> GetColumnMappings(
            EntityType entityType, EntitySetMapping entitySetMapping)
        {
            DebugCheck.NotNull(entityType);
            DebugCheck.NotNull(entitySetMapping);

            return new[] { entityType }
                .Concat(GetParents(entityType))
                .SelectMany(
                    et => entitySetMapping
                              .TypeMappings
                              .Where(stm => stm.Types.Contains(et))
                              .SelectMany(stm => stm.MappingFragments)
                              .SelectMany(mf => mf.ColumnMappings));
        }

        public void Generate(AssociationSetMapping associationSetMapping, DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(associationSetMapping);
            DebugCheck.NotNull(databaseMapping);

            var iaFkProperties = GetIndependentFkColumns(associationSetMapping).ToList();
            var sourceEntityType = associationSetMapping.AssociationSet.ElementType.SourceEnd.GetEntityType();
            var targetEntityType = associationSetMapping.AssociationSet.ElementType.TargetEnd.GetEntityType();
            var functionNamePrefix = sourceEntityType.Name + targetEntityType.Name;

            var insertFunctionMapping
                = GenerateFunctionMapping(
                    ModificationOperator.Insert,
                    associationSetMapping.AssociationSet,
                    associationSetMapping.AssociationSet.ElementType,
                    databaseMapping,
                    Enumerable.Empty<EdmProperty>(),
                    iaFkProperties,
                    new ColumnMappingBuilder[0],
                    functionNamePrefix: functionNamePrefix);

            var deleteFunctionMapping
                = GenerateFunctionMapping(
                    ModificationOperator.Delete,
                    associationSetMapping.AssociationSet,
                    associationSetMapping.AssociationSet.ElementType,
                    databaseMapping,
                    Enumerable.Empty<EdmProperty>(),
                    iaFkProperties,
                    new ColumnMappingBuilder[0],
                    functionNamePrefix: functionNamePrefix);

            associationSetMapping.ModificationFunctionMapping
                = new AssociationSetModificationFunctionMapping(
                    associationSetMapping.AssociationSet,
                    deleteFunctionMapping,
                    insertFunctionMapping);
        }

        private static IEnumerable<Tuple<ModificationFunctionMemberPath, EdmProperty>> GetIndependentFkColumns(
            AssociationSetMapping associationSetMapping)
        {
            DebugCheck.NotNull(associationSetMapping);

            foreach (var propertyMapping in associationSetMapping.SourceEndMapping.PropertyMappings)
            {
                yield return
                    Tuple.Create(
                        new ModificationFunctionMemberPath(
                            new EdmMember[] { propertyMapping.Property, associationSetMapping.SourceEndMapping.AssociationEnd },
                            associationSetMapping.AssociationSet), propertyMapping.Column);
            }

            foreach (var propertyMapping in associationSetMapping.TargetEndMapping.PropertyMappings)
            {
                yield return
                    Tuple.Create(
                        new ModificationFunctionMemberPath(
                            new EdmMember[] { propertyMapping.Property, associationSetMapping.TargetEndMapping.AssociationEnd },
                            associationSetMapping.AssociationSet), propertyMapping.Column);
            }
        }

        private static IEnumerable<Tuple<ModificationFunctionMemberPath, EdmProperty>> GetIndependentFkColumns(
            EntityType entityType, DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(entityType);
            DebugCheck.NotNull(databaseMapping);

            foreach (var associationSetMapping in databaseMapping.GetAssociationSetMappings())
            {
                var associationType = associationSetMapping.AssociationSet.ElementType;

                if (associationType.IsManyToMany())
                {
                    continue;
                }

                AssociationEndMember _, dependentEnd;
                if (!associationType.TryGuessPrincipalAndDependentEnds(out _, out dependentEnd))
                {
                    dependentEnd = associationType.TargetEnd;
                }

                var dependentEntityType = dependentEnd.GetEntityType();

                if (dependentEntityType == entityType
                    || GetParents(entityType).Contains(dependentEntityType))
                {
                    var endPropertyMapping
                        = associationSetMapping.TargetEndMapping.AssociationEnd != dependentEnd
                              ? associationSetMapping.TargetEndMapping
                              : associationSetMapping.SourceEndMapping;

                    foreach (var propertyMapping in endPropertyMapping.PropertyMappings)
                    {
                        yield return
                            Tuple.Create(
                                new ModificationFunctionMemberPath(
                                    new EdmMember[] { propertyMapping.Property, dependentEnd },
                                    associationSetMapping.AssociationSet), propertyMapping.Column);
                    }
                }
            }
        }

        private static IEnumerable<EntityType> GetParents(EntityType entityType)
        {
            DebugCheck.NotNull(entityType);

            while (entityType.BaseType != null)
            {
                yield return (EntityType)entityType.BaseType;

                entityType = (EntityType)entityType.BaseType;
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private ModificationFunctionMapping GenerateFunctionMapping(
            ModificationOperator modificationOperator,
            EntitySetBase entitySetBase,
            EntityTypeBase entityTypeBase,
            DbDatabaseMapping databaseMapping,
            IEnumerable<EdmProperty> parameterProperties,
            IEnumerable<Tuple<ModificationFunctionMemberPath, EdmProperty>> iaFkProperties,
            IList<ColumnMappingBuilder> columnMappings,
            IEnumerable<EdmProperty> resultProperties = null,
            string functionNamePrefix = null)
        {
            DebugCheck.NotNull(entitySetBase);
            DebugCheck.NotNull(entityTypeBase);
            DebugCheck.NotNull(databaseMapping);
            DebugCheck.NotNull(parameterProperties);
            DebugCheck.NotNull(iaFkProperties);
            DebugCheck.NotNull(columnMappings);

            var useOriginalValues = modificationOperator == ModificationOperator.Delete;

            var parameterMappingGenerator
                = new FunctionParameterMappingGenerator(_providerManifest);

            var parameterBindings
                = parameterMappingGenerator
                    .Generate(
                        modificationOperator == ModificationOperator.Insert
                            && IsTableSplitDependent(entityTypeBase, databaseMapping)
                                ? ModificationOperator.Update 
                                : modificationOperator,
                        parameterProperties,
                        columnMappings,
                        new List<EdmProperty>(),
                        useOriginalValues)
                    .Concat(
                        parameterMappingGenerator
                            .Generate(iaFkProperties, useOriginalValues))
                    .ToList();

            var parameters
                = parameterBindings.Select(b => b.Parameter).ToList();

            UniquifyParameterNames(parameters);

            var functionPayload
                = new EdmFunctionPayload
                      {
                          ReturnParameters = new FunctionParameter[0],
                          Parameters = parameters.ToArray(),
                          IsComposable = false
                      };

            var function
                = databaseMapping.Database
                    .AddFunction(
                        (functionNamePrefix ?? entityTypeBase.Name) + "_" + modificationOperator.ToString(),
                        functionPayload);

            var functionMapping
                = new ModificationFunctionMapping(
                    entitySetBase,
                    entityTypeBase,
                    function,
                    parameterBindings,
                    null,
                    resultProperties != null
                        ? resultProperties.Select(
                            p => new ModificationFunctionResultBinding(
                                     columnMappings.First(cm => cm.PropertyPath.SequenceEqual(new[] { p })).ColumnProperty.Name,
                                     p))
                        : null);

            return functionMapping;
        }

        private static bool IsTableSplitDependent(EntityTypeBase entityTypeBase, DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(entityTypeBase);

            var associationType
                = databaseMapping
                    .Model.AssociationTypes
                    .SingleOrDefault(
                        at => at.IsForeignKey
                              && at.IsRequiredToRequired()
                              && !at.IsSelfReferencing()
                              && (at.SourceEnd.GetEntityType().IsAssignableFrom(entityTypeBase)
                                  || at.TargetEnd.GetEntityType().IsAssignableFrom(entityTypeBase))
                              && databaseMapping.Database.AssociationTypes
                                  .All(fk => fk.Name != at.Name)); // no store FK == shared table

            return associationType != null
                   && associationType.TargetEnd.GetEntityType() == entityTypeBase;
        }

        private static void UniquifyParameterNames(IList<FunctionParameter> parameters)
        {
            DebugCheck.NotNull(parameters);

            foreach (var parameter in parameters)
            {
                parameter.Name = parameters.Except(new[] { parameter }).UniquifyName(parameter.Name);
            }
        }
    }
}
