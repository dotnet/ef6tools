// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Represents a mapping from a model function import to a store non-composable function.
    /// </summary>
    public sealed class FunctionImportMappingNonComposable : FunctionImportMapping
    {
        private readonly ReadOnlyCollection<FunctionImportResultMapping> _resultMappings;

        /// <summary>
        /// Initializes a new FunctionImportMappingNonComposable instance.
        /// </summary>
        /// <param name="functionImport">The model function import.</param>
        /// <param name="targetFunction">The store non-composable function.</param>
        /// <param name="resultMappings">The function import result mappings.</param>
        /// <param name="containerMapping">The parent container mapping.</param>
        public FunctionImportMappingNonComposable(
            EdmFunction functionImport,
            EdmFunction targetFunction,
            IEnumerable<FunctionImportResultMapping> resultMappings,
            EntityContainerMapping containerMapping)
            : base(
                Check.NotNull(functionImport, "functionImport"),
                Check.NotNull(targetFunction, "targetFunction"))
        {
            Check.NotNull(resultMappings, "resultMappings");
            Check.NotNull(containerMapping, "containerMapping");

            Debug.Assert(!functionImport.IsComposableAttribute);
            Debug.Assert(!targetFunction.IsComposableAttribute);

 
            if (!resultMappings.Any())
            {
                // when this method is invoked when a CodeFirst model is being built (e.g. from a custom convention) the
                // StorageMappingItemCollection will be null. In this case we can provide an empty EdmItemCollection which
                // will allow inferring implicit result mapping
                var edmItemCollection = containerMapping.StorageMappingItemCollection != null
                    ? containerMapping.StorageMappingItemCollection.EdmItemCollection
                    : new EdmItemCollection(new EdmModel(DataSpace.CSpace));

                _internalResultMappings = new ReadOnlyCollection<FunctionImportStructuralTypeMappingKB>(
                    new[]
                        {
                            new FunctionImportStructuralTypeMappingKB(
                                new List<FunctionImportStructuralTypeMapping>(), 
                                edmItemCollection)
                        });
                noExplicitResultMappings = true;
            }
            else
            {
                Debug.Assert(functionImport.ReturnParameters.Count == resultMappings.Count());

                _internalResultMappings = new ReadOnlyCollection<FunctionImportStructuralTypeMappingKB>(
                    resultMappings
                        .Select(
                            resultMapping => new FunctionImportStructuralTypeMappingKB(
                                                    resultMapping.TypeMappings,
                                                    containerMapping.StorageMappingItemCollection.EdmItemCollection))
                        .ToArray());

                noExplicitResultMappings = false;
            }

            _resultMappings = new ReadOnlyCollection<FunctionImportResultMapping>(resultMappings.ToList());
        }

        internal FunctionImportMappingNonComposable(
            EdmFunction functionImport,
            EdmFunction targetFunction,
            List<List<FunctionImportStructuralTypeMapping>> structuralTypeMappingsList,
            ItemCollection itemCollection)
            : base(functionImport, targetFunction)
        {
            DebugCheck.NotNull(structuralTypeMappingsList);
            DebugCheck.NotNull(itemCollection);
            Debug.Assert(!functionImport.IsComposableAttribute, "!functionImport.IsComposableAttribute");
            Debug.Assert(!targetFunction.IsComposableAttribute, "!targetFunction.IsComposableAttribute");

            if (structuralTypeMappingsList.Count == 0)
            {
                _internalResultMappings = new ReadOnlyCollection<FunctionImportStructuralTypeMappingKB>(
                    new[]
                        {
                            new FunctionImportStructuralTypeMappingKB(new List<FunctionImportStructuralTypeMapping>(), itemCollection)
                        });
                noExplicitResultMappings = true;
            }
            else
            {
                Debug.Assert(functionImport.ReturnParameters.Count == structuralTypeMappingsList.Count);
                _internalResultMappings = new ReadOnlyCollection<FunctionImportStructuralTypeMappingKB>(
                    structuralTypeMappingsList
                        .Select(
                            structuralTypeMappings => new FunctionImportStructuralTypeMappingKB(
                                                            structuralTypeMappings,
                                                            itemCollection))
                        .ToArray());
                noExplicitResultMappings = false;
            }
        }

        private readonly bool noExplicitResultMappings;

        // <summary>
        // Gets function import return type mapping knowledge bases.
        // </summary>
        private readonly ReadOnlyCollection<FunctionImportStructuralTypeMappingKB> _internalResultMappings;

        internal ReadOnlyCollection<FunctionImportStructuralTypeMappingKB> InternalResultMappings
        {
            get { return _internalResultMappings; }
        }

        /// <summary>
        /// Gets the function import result mappings.
        /// </summary>
        public ReadOnlyCollection<FunctionImportResultMapping> ResultMappings
        {
            get { return _resultMappings; }
        }

        internal override void SetReadOnly()
        {
            SetReadOnly(_resultMappings);

            base.SetReadOnly();
        }

        // <summary>
        // If no return mappings were specified in the MSL return an empty return type mapping knowledge base.
        // Otherwise return the resultSetIndexth return type mapping knowledge base, or throw if resultSetIndex is out of range
        // </summary>
        internal FunctionImportStructuralTypeMappingKB GetResultMapping(int resultSetIndex)
        {
            Debug.Assert(resultSetIndex >= 0, "resultSetIndex >= 0");
            if (noExplicitResultMappings)
            {
                Debug.Assert(InternalResultMappings.Count == 1, "this.InternalResultMappings.Count == 1");
                return InternalResultMappings[0];
            }
            else
            {
                if (InternalResultMappings.Count <= resultSetIndex)
                {
                    throw new ArgumentOutOfRangeException("resultSetIndex");
                }
                return InternalResultMappings[resultSetIndex];
            }
        }

        // <summary>
        // Gets the disctriminator columns resultSetIndexth result set, or an empty array if the index is not in range
        // </summary>
        internal IList<string> GetDiscriminatorColumns(int resultSetIndex)
        {
            var resultMapping = GetResultMapping(resultSetIndex);
            return resultMapping.DiscriminatorColumns;
        }

        // <summary>
        // Given discriminator values (ordinally aligned with DiscriminatorColumns), determines
        // the entity type to return. Throws a CommandExecutionException if the type is ambiguous.
        // </summary>
        internal EntityType Discriminate(object[] discriminatorValues, int resultSetIndex)
        {
            var resultMapping = GetResultMapping(resultSetIndex);
            Debug.Assert(resultMapping != null);

            // initialize matching types bit map
            var typeCandidates = new BitArray(resultMapping.MappedEntityTypes.Count, true);

            foreach (var typeMapping in resultMapping.NormalizedEntityTypeMappings)
            {
                // check if this type mapping is matched
                var matches = true;
                var columnConditions = typeMapping.ColumnConditions;
                for (var i = 0; i < columnConditions.Count; i++)
                {
                    if (null != columnConditions[i]
                        && // this discriminator doesn't matter for the given condition
                        !columnConditions[i].ColumnValueMatchesCondition(discriminatorValues[i]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    // if the type condition is met, narrow the set of type candidates
                    typeCandidates = typeCandidates.And(typeMapping.ImpliedEntityTypes);
                }
                else
                {
                    // if the type condition fails, all implied types are eliminated
                    // (the type mapping fragment is a co-implication, so a type is no longer
                    // a candidate if any condition referring to it is false)
                    typeCandidates = typeCandidates.And(typeMapping.ComplementImpliedEntityTypes);
                }
            }

            // find matching type condition
            EntityType entityType = null;
            for (var i = 0; i < typeCandidates.Length; i++)
            {
                if (typeCandidates[i])
                {
                    if (null != entityType)
                    {
                        throw new EntityCommandExecutionException(Strings.ADP_InvalidDataReaderUnableToDetermineType);
                    }
                    entityType = resultMapping.MappedEntityTypes[i];
                }
            }

            // if there is no match, raise an exception
            if (null == entityType)
            {
                throw new EntityCommandExecutionException(Strings.ADP_InvalidDataReaderUnableToDetermineType);
            }

            return entityType;
        }

        // <summary>
        // Determines the expected shape of store results. We expect a column for every property
        // of the mapped type (or types) and a column for every discriminator column. We make no
        // assumptions about the order of columns: the provider is expected to determine appropriate
        // types by looking at the names of the result columns, not the order of columns, which is
        // different from the typical handling of row types in the EF.
        // </summary>
        // <remarks>
        // Requires that the given function import mapping refers to a Collection(Entity) or Collection(ComplexType) CSDL
        // function.
        // </remarks>
        // <returns> Row type. </returns>
        internal TypeUsage GetExpectedTargetResultType(int resultSetIndex)
        {
            var resultMapping = GetResultMapping(resultSetIndex);

            // Collect all columns as name-type pairs.
            var columns = new Dictionary<string, TypeUsage>();

            // Figure out which entity types we expect to yield from the function.
            IEnumerable<StructuralType> structuralTypes;
            if (0 == resultMapping.NormalizedEntityTypeMappings.Count)
            {
                // No explicit type mappings; just use the type specified in the ReturnType attribute on the function.
                StructuralType structuralType;
                MetadataHelper.TryGetFunctionImportReturnType(FunctionImport, resultSetIndex, out structuralType);
                Debug.Assert(null != structuralType, "this method must be called only for entity/complextype reader function imports");
                structuralTypes = new[] { structuralType };
            }
            else
            {
                // Types are explicitly mapped.
                structuralTypes = resultMapping.MappedEntityTypes.Cast<StructuralType>();
            }

            // Gather columns corresponding to all properties.
            foreach (var structuralType in structuralTypes)
            {
                foreach (EdmProperty property in TypeHelpers.GetAllStructuralMembers(structuralType))
                {
                    // NOTE: if a complex type is encountered, the column map generator will
                    // throw. For now, we just let them through.

                    // We expect to see each property multiple times, so we use indexer rather than
                    // .Add.
                    columns[property.Name] = property.TypeUsage;
                }
            }

            // Gather discriminator columns.
            foreach (var discriminatorColumn in GetDiscriminatorColumns(resultSetIndex))
            {
                if (!columns.ContainsKey(discriminatorColumn))
                {
                    // CONSIDER: we assume that discriminatorColumns are all string types. In practice,
                    // we're flexible about the runtime type during materialization, so the provider's
                    // decision is hopefully irrelevant. The alternative is to require typed stored
                    // procedure declarations in the SSDL, which is too much of a burden on the user and/or the
                    // tools (there is no reliable way of determining this metadata automatically from SQL
                    // Server).

                    var type = TypeUsage.CreateStringTypeUsage(
                        MetadataWorkspace.GetModelPrimitiveType(PrimitiveTypeKind.String), true, false);
                    columns.Add(discriminatorColumn, type);
                }
            }

            // Expected type is a collection of rows
            var rowType = new RowType(columns.Select(c => new EdmProperty(c.Key, c.Value)));
            var result = TypeUsage.Create(new CollectionType(TypeUsage.Create(rowType)));
            return result;
        }
    }
}
