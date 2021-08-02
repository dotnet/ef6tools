// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.QueryCache
{
    using System.Collections.Concurrent;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Core.Objects.Internal;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    // <summary>
    // Represents a compiled LINQ ObjectQuery cache entry
    // </summary>
    internal sealed class CompiledQueryCacheEntry : QueryCacheEntry
    {
        // <summary>
        // The merge option that was inferred during expression conversion.
        // </summary>
        public readonly MergeOption? PropagatedMergeOption;

        // <summary>
        // A dictionary that contains a plan for each combination of
        // merge option and UseCSharpNullComparisonBehavior flag.
        // </summary>
        private readonly ConcurrentDictionary<String, ObjectQueryExecutionPlan> _plans;

        #region Constructors

        // <summary>
        // constructor
        // </summary>
        // <param name="queryCacheKey"> The cache key that targets this cache entry </param>
        // <param name="mergeOption"> The inferred merge option that applies to this cached query </param>
        internal CompiledQueryCacheEntry(QueryCacheKey queryCacheKey, MergeOption? mergeOption)
            : base(queryCacheKey, null)
        {
            PropagatedMergeOption = mergeOption;
            _plans = new ConcurrentDictionary<string, ObjectQueryExecutionPlan>();
        }

        #endregion

        #region Methods/Properties

        // <summary>
        // Retrieves the execution plan for the specified merge option and UseCSharpNullComparisonBehavior flag. May return null if the
        // plan for the given merge option and useCSharpNullComparisonBehavior flag is not present.
        // </summary>
        // <param name="mergeOption"> The merge option for which an execution plan is required. </param>
        // <param name="useCSharpNullComparisonBehavior"> Flag indicating if C# behavior should be used for null comparisons. </param>
        // <returns>
        // The corresponding execution plan, if it exists; otherwise <c>null</c> .
        // </returns>
        internal ObjectQueryExecutionPlan GetExecutionPlan(MergeOption mergeOption, bool useCSharpNullComparisonBehavior)
        {
            var key = GenerateLocalCacheKey(mergeOption, useCSharpNullComparisonBehavior);
            ObjectQueryExecutionPlan plan;
            _plans.TryGetValue(key, out plan);
            return plan;
        }

        // <summary>
        // Attempts to set the execution plan for <paramref name="newPlan" />'s merge option and
        // <paramref
        //     name="useCSharpNullComparisonBehavior" />
        // flag on
        // this cache entry to <paramref name="newPlan" />. If a plan already exists for that merge option and UseCSharpNullComparisonBehavior flag, the
        // current value is not changed but is returned to the caller. Otherwise <paramref name="newPlan" /> is returned to the caller.
        // </summary>
        // <param name="newPlan"> The new execution plan to add to this cache entry. </param>
        // <param name="useCSharpNullComparisonBehavior"> Flag indicating if C# behavior should be used for null comparisons. </param>
        // <returns>
        // The execution plan that corresponds to <paramref name="newPlan" /> 's merge option, which may be
        // <paramref
        //     name="newPlan" />
        // or may be a previously added execution plan.
        // </returns>
        internal ObjectQueryExecutionPlan SetExecutionPlan(ObjectQueryExecutionPlan newPlan, bool useCSharpNullComparisonBehavior)
        {
            DebugCheck.NotNull(newPlan);

            var planKey = GenerateLocalCacheKey(newPlan.MergeOption, useCSharpNullComparisonBehavior);
            // Get the value if it is there. If not, add it and get it.
            return (_plans.GetOrAdd(planKey, newPlan));
        }

        // <summary>
        // Convenience method to retrieve the result type from the first non-null execution plan found on this cache entry.
        // </summary>
        // <param name="resultType"> The result type of any execution plan that is or could be added to this cache entry </param>
        // <returns>
        // <c>true</c> if at least one execution plan was present and a result type could be retrieved; otherwise <c>false</c>
        // </returns>
        internal bool TryGetResultType(out TypeUsage resultType)
        {
            foreach (var value in _plans.Values)
            {
                resultType = value.ResultType;
                return true;
            }
            resultType = null;
            return false;
        }

        #endregion

        internal override object GetTarget()
        {
            return this;
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private static string GenerateLocalCacheKey(MergeOption mergeOption, bool useCSharpNullComparisonBehavior)
        {
            switch (mergeOption)
            {
                case MergeOption.AppendOnly:
                case MergeOption.NoTracking:
                case MergeOption.OverwriteChanges:
                case MergeOption.PreserveChanges:
                    return string.Join("", Enum.GetName(typeof(MergeOption), mergeOption), useCSharpNullComparisonBehavior);
                default:
                    throw new ArgumentOutOfRangeException("newPlan.MergeOption");
            }
        }
    }
}
