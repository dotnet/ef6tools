// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.PlanCompiler
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Query.InternalTrees;

    // <summary>
    // Finds the record (Row) types that we're projecting out of the query, and
    // ensures that we mark them as needing a nullable sentinel, so when we
    // flatten them later we'll have one added.
    // </summary>
    internal class StructuredTypeNullabilityAnalyzer : ColumnMapVisitor<HashSet<string>>
    {
        internal static StructuredTypeNullabilityAnalyzer Instance = new StructuredTypeNullabilityAnalyzer();

        // <summary>
        // VarRefColumnMap
        // </summary>
        internal override void Visit(VarRefColumnMap columnMap, HashSet<string> typesNeedingNullSentinel)
        {
            AddTypeNeedingNullSentinel(typesNeedingNullSentinel, columnMap.Type);
            base.Visit(columnMap, typesNeedingNullSentinel);
        }

        // <summary>
        // Recursively add any Row types to the list of types needing a sentinel.
        // </summary>
        private static void AddTypeNeedingNullSentinel(HashSet<string> typesNeedingNullSentinel, TypeUsage typeUsage)
        {
            if (TypeSemantics.IsCollectionType(typeUsage))
            {
                AddTypeNeedingNullSentinel(typesNeedingNullSentinel, TypeHelpers.GetElementTypeUsage(typeUsage));
            }
            else
            {
                if (TypeSemantics.IsRowType(typeUsage)
                    || TypeSemantics.IsComplexType(typeUsage))
                {
                    MarkAsNeedingNullSentinel(typesNeedingNullSentinel, typeUsage);
                }
                foreach (EdmMember m in TypeHelpers.GetAllStructuralMembers(typeUsage))
                {
                    AddTypeNeedingNullSentinel(typesNeedingNullSentinel, m.TypeUsage);
                }
            }
        }

        // <summary>
        // Marks the given typeUsage as needing a null sentinel.
        // Call this method instead of calling Add over the HashSet directly, to ensure consistency.
        // </summary>
        internal static void MarkAsNeedingNullSentinel(HashSet<string> typesNeedingNullSentinel, TypeUsage typeUsage)
        {
            typesNeedingNullSentinel.Add(typeUsage.EdmType.Identity);
        }
    }
}
