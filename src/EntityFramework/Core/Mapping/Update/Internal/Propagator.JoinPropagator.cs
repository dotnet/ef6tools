// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.Update.Internal
{
    // We use CompositeKey on both sides of the dictionary because it is used both to identify rows that should be 
    // joined (the Key part) and to carry context about the rows being joined (e.g. which components of the row 
    // correspond to the join key).
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using JoinDictionary = System.Collections.Generic.Dictionary<CompositeKey, Tuple<CompositeKey, PropagatorResult>>;

    internal partial class Propagator
    {
        // <summary>
        // Performs join propagation. The basic strategy is to identify changes (inserts, deletes)
        // on either side of the join that are related according to the join criteria. Support is restricted
        // to conjunctions of equality predicates of the form <c>left property == right property</c>.
        // When a group of related changes is identified, rules are applied based on the existence of
        // different components (e.g., a left insert + right insert).
        // </summary>
        // <remarks>
        // The joins handled by this class are degenerate in the sense that a row in the 'left' input always
        // joins with at most one row in the 'right' input. The restrictions that allow for this assumption
        // are described in the update design spec (see 'Level 5 Optimization').
        // </remarks>
        // <remarks>
        // Propagation rules for joins are stored in static fields of the class (initialized in the static
        // constructor for the class).
        // </remarks>
        private partial class JoinPropagator
        {
            // <summary>
            // Constructs a join propagator.
            // </summary>
            // <param name="left"> Result of propagating changes in the left input to the join </param>
            // <param name="right"> Result of propagating changes in the right input to the join </param>
            // <param name="node"> Join operator in update mapping view over which to propagate changes </param>
            // <param name="parent"> Handler of propagation for the entire update mapping view </param>
            internal JoinPropagator(ChangeNode left, ChangeNode right, DbJoinExpression node, Propagator parent)
            {
                DebugCheck.NotNull(left);
                DebugCheck.NotNull(right);
                DebugCheck.NotNull(node);
                DebugCheck.NotNull(parent);

                m_left = left;
                m_right = right;
                m_joinExpression = node;
                m_parent = parent;

                Debug.Assert(
                    DbExpressionKind.LeftOuterJoin == node.ExpressionKind || DbExpressionKind.InnerJoin == node.ExpressionKind,
                    "(Update/JoinPropagagtor/JoinEvaluator) " +
                    "caller must ensure only left outer and inner joins are requested");
                // Retrieve propagation rules for the join type of the expression.
                if (DbExpressionKind.InnerJoin
                    == m_joinExpression.ExpressionKind)
                {
                    m_insertRules = _innerJoinInsertRules;
                    m_deleteRules = _innerJoinDeleteRules;
                }
                else
                {
                    m_insertRules = _leftOuterJoinInsertRules;
                    m_deleteRules = _leftOuterJoinDeleteRules;
                }

                // Figure out key selectors involved in the equi-join (if it isn't an equi-join, we don't support it)
                JoinConditionVisitor.GetKeySelectors(node.JoinCondition, out m_leftKeySelectors, out m_rightKeySelectors);

                // Find the key selector expressions in the left and right placeholders
                m_leftPlaceholderKey = ExtractKey(m_left.Placeholder, m_leftKeySelectors);
                m_rightPlaceholderKey = ExtractKey(m_right.Placeholder, m_rightKeySelectors);
            }

            /*
* These static dictionaries are initialized by the static constructor for this class.
* They describe for each combination of input elements (the key) propagation rules, which
* are expressions over the input expressions.
* */
            private static readonly Dictionary<Ops, Ops> _innerJoinInsertRules;
            private static readonly Dictionary<Ops, Ops> _innerJoinDeleteRules;
            private static readonly Dictionary<Ops, Ops> _leftOuterJoinInsertRules;
            private static readonly Dictionary<Ops, Ops> _leftOuterJoinDeleteRules;

            private readonly DbJoinExpression m_joinExpression;
            private readonly Propagator m_parent;
            private readonly Dictionary<Ops, Ops> m_insertRules;
            private readonly Dictionary<Ops, Ops> m_deleteRules;
            private readonly ReadOnlyCollection<DbExpression> m_leftKeySelectors;
            private readonly ReadOnlyCollection<DbExpression> m_rightKeySelectors;
            private readonly ChangeNode m_left;
            private readonly ChangeNode m_right;
            private readonly CompositeKey m_leftPlaceholderKey;
            private readonly CompositeKey m_rightPlaceholderKey;

            // <summary>
            // Initialize rules.
            // </summary>
            [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
            static JoinPropagator()
            {
                _innerJoinInsertRules = new Dictionary<Ops, Ops>(EqualityComparer<Ops>.Default);
                _innerJoinDeleteRules = new Dictionary<Ops, Ops>(EqualityComparer<Ops>.Default);
                _leftOuterJoinInsertRules = new Dictionary<Ops, Ops>(EqualityComparer<Ops>.Default);
                _leftOuterJoinDeleteRules = new Dictionary<Ops, Ops>(EqualityComparer<Ops>.Default);

                #region Initialize propagation rules

                // These rules are taken from the mapping.update.design.doc, Section 3.5.1.3
                // <Input, 
                //  Inner join insert rule, 
                //  Inner join delete rule, 
                //  Left outer join insert rule, 
                //  Left outer join delete rule>
                InitializeRule(
                    Ops.LeftUpdate | Ops.RightUpdate,
                    Ops.LeftInsertJoinRightInsert,
                    Ops.LeftDeleteJoinRightDelete,
                    Ops.LeftInsertJoinRightInsert,
                    Ops.LeftDeleteJoinRightDelete);

                InitializeRule(
                    Ops.LeftDelete | Ops.RightDelete,
                    Ops.Nothing,
                    Ops.LeftDeleteJoinRightDelete,
                    Ops.Nothing,
                    Ops.LeftDeleteJoinRightDelete);

                InitializeRule(
                    Ops.LeftInsert | Ops.RightInsert,
                    Ops.LeftInsertJoinRightInsert,
                    Ops.Nothing,
                    Ops.LeftInsertJoinRightInsert,
                    Ops.Nothing);

                InitializeRule(
                    Ops.LeftUpdate,
                    Ops.LeftInsertUnknownExtended,
                    Ops.LeftDeleteUnknownExtended,
                    Ops.LeftInsertUnknownExtended,
                    Ops.LeftDeleteUnknownExtended);

                InitializeRule(
                    Ops.RightUpdate,
                    Ops.RightInsertUnknownExtended,
                    Ops.RightDeleteUnknownExtended,
                    Ops.RightInsertUnknownExtended,
                    Ops.RightDeleteUnknownExtended);

                InitializeRule(
                    Ops.LeftUpdate | Ops.RightDelete,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.LeftInsertNullModifiedExtended,
                    Ops.LeftDeleteJoinRightDelete);

                InitializeRule(
                    Ops.LeftUpdate | Ops.RightInsert,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.LeftInsertJoinRightInsert,
                    Ops.LeftDeleteNullModifiedExtended);

                InitializeRule(
                    Ops.LeftDelete,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Nothing,
                    Ops.LeftDeleteNullPreserveExtended);

                InitializeRule(
                    Ops.LeftInsert,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.LeftInsertNullModifiedExtended,
                    Ops.Nothing);

                InitializeRule(
                    Ops.RightDelete,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.LeftUnknownNullModifiedExtended,
                    Ops.RightDeleteUnknownExtended);

                InitializeRule(
                    Ops.RightInsert,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.RightInsertUnknownExtended,
                    Ops.LeftUnknownNullModifiedExtended);

                InitializeRule(
                    Ops.LeftDelete | Ops.RightUpdate,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported);

                InitializeRule(
                    Ops.LeftDelete | Ops.RightInsert,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported);

                InitializeRule(
                    Ops.LeftInsert | Ops.RightUpdate,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported);

                InitializeRule(
                    Ops.LeftInsert | Ops.RightDelete,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported,
                    Ops.Unsupported);

                #endregion
            }

            // <summary>
            // Initializes propagation rules for a specific input combination.
            // </summary>
            // <param name="input"> Describes the elements available in the input </param>
            // <param name="joinInsert"> Describes the rule for inserts when the operator is an inner join </param>
            // <param name="joinDelete"> Describes the rule for deletes when the operator is an inner join </param>
            // <param name="lojInsert"> Describes the rule for inserts when the operator is a left outer join </param>
            // <param name="lojDelete"> Describes the rule for deletes when the operator is a left outer join </param>
            private static void InitializeRule(Ops input, Ops joinInsert, Ops joinDelete, Ops lojInsert, Ops lojDelete)
            {
                _innerJoinInsertRules.Add(input, joinInsert);
                _innerJoinDeleteRules.Add(input, joinDelete);
                _leftOuterJoinInsertRules.Add(input, lojInsert);
                _leftOuterJoinDeleteRules.Add(input, lojDelete);

                // Ensure that the right hand side of each rule contains no requests for specific row values
                // that are not also in the input.
                Debug.Assert(
                    (((joinInsert | joinDelete | lojInsert | lojDelete) &
                      (Ops.LeftInsert | Ops.LeftDelete | Ops.RightInsert | Ops.RightDelete)) & (~input)) == Ops.Nothing,
                    "(Update/JoinPropagator/Initialization) Rules can't use unavailable data");

                // An unknown value can appear in both the delete and insert rule result or neither.
                Debug.Assert(
                    ((joinInsert ^ joinDelete) & (Ops.LeftUnknown | Ops.RightUnknown)) == Ops.Nothing &&
                    ((lojInsert ^ lojDelete) & (Ops.LeftUnknown | Ops.RightUnknown)) == Ops.Nothing,
                    "(Update/JoinPropagator/Initialization) Unknowns must appear in both delete and insert rules " +
                    "or in neither (in other words, for updates only)");
            }

            // <summary>
            // Performs join propagation.
            // </summary>
            // <returns> Changes propagated to the current join node in the update mapping view. </returns>
            internal ChangeNode Propagate()
            {
                // Construct an empty change node for the result
                var result = BuildChangeNode(m_joinExpression);

                // Gather all keys involved in the join
                var leftDeletes = ProcessKeys(m_left.Deleted, m_leftKeySelectors);
                var leftInserts = ProcessKeys(m_left.Inserted, m_leftKeySelectors);
                var rightDeletes = ProcessKeys(m_right.Deleted, m_rightKeySelectors);
                var rightInserts = ProcessKeys(m_right.Inserted, m_rightKeySelectors);
                var allKeys = leftDeletes.Keys
                                         .Concat(leftInserts.Keys)
                                         .Concat(rightDeletes.Keys)
                                         .Concat(rightInserts.Keys)
                                         .Distinct(m_parent.UpdateTranslator.KeyComparer);

                // Perform propagation one key at a time
                foreach (var key in allKeys)
                {
                    Propagate(key, result, leftDeletes, leftInserts, rightDeletes, rightInserts);
                }

                // Construct a new placeholder (see ChangeNode.Placeholder) for the join result node.
                result.Placeholder = CreateResultTuple(
                    Tuple.Create((CompositeKey)null, m_left.Placeholder), Tuple.Create((CompositeKey)null, m_right.Placeholder), result);

                return result;
            }

            // <summary>
            // Propagate all changes associated with a particular join key.
            // </summary>
            // <param name="key"> Key. </param>
            // <param name="result"> Resulting changes are added to this result. </param>
            private void Propagate(
                CompositeKey key, ChangeNode result, JoinDictionary leftDeletes, JoinDictionary leftInserts,
                JoinDictionary rightDeletes, JoinDictionary rightInserts)
            {
                // Retrieve changes associates with this join key
                Tuple<CompositeKey, PropagatorResult> leftInsert = null;
                Tuple<CompositeKey, PropagatorResult> leftDelete = null;
                Tuple<CompositeKey, PropagatorResult> rightInsert = null;
                Tuple<CompositeKey, PropagatorResult> rightDelete = null;

                var input = Ops.Nothing;

                if (leftInserts.TryGetValue(key, out leftInsert))
                {
                    input |= Ops.LeftInsert;
                }
                if (leftDeletes.TryGetValue(key, out leftDelete))
                {
                    input |= Ops.LeftDelete;
                }
                if (rightInserts.TryGetValue(key, out rightInsert))
                {
                    input |= Ops.RightInsert;
                }
                if (rightDeletes.TryGetValue(key, out rightDelete))
                {
                    input |= Ops.RightDelete;
                }

                // Get propagation rules for the changes
                var insertRule = m_insertRules[input];
                var deleteRule = m_deleteRules[input];

                if (Ops.Unsupported == insertRule
                    || Ops.Unsupported == deleteRule)
                {
                    // If no propagation rules are defined, it suggests an invalid workload (e.g.
                    // a required entity or relationship is missing). In general, such exceptions
                    // should be caught by the RelationshipConstraintValidator, but we defensively
                    // check for problems here regardless. For instance, a 0..1:1..1 self-assocation
                    // implied a stronger constraint that cannot be checked by RelationshipConstraintValidator.

                    // First gather state entries contributing to the problem
                    var stateEntries = new List<IEntityStateEntry>();
                    Action<Tuple<CompositeKey, PropagatorResult>> addStateEntries = (r) =>
                        {
                            if (r != null)
                            {
                                stateEntries.AddRange(
                                    SourceInterpreter.GetAllStateEntries(
                                        r.Item2, m_parent.m_updateTranslator,
                                        m_parent.m_table));
                            }
                        };
                    addStateEntries(leftInsert);
                    addStateEntries(leftDelete);
                    addStateEntries(rightInsert);
                    addStateEntries(rightDelete);

                    throw new UpdateException(Strings.Update_InvalidChanges, null, stateEntries.Cast<ObjectStateEntry>().Distinct());
                }

                // Where needed, substitute null/unknown placeholders. In some of the join propagation
                // rules, we handle the case where a side of the join is 'unknown', or where one side
                // of a join is comprised of an record containing only nulls. For instance, we may update
                // only one extent appearing in a row of a table (unknown), or; we may insert only
                // the left hand side of a left outer join, in which case the right hand side is 'null'.
                if (0 != (Ops.LeftUnknown & insertRule))
                {
                    leftInsert = Tuple.Create(key, LeftPlaceholder(key, PopulateMode.Unknown));
                }
                if (0 != (Ops.LeftUnknown & deleteRule))
                {
                    leftDelete = Tuple.Create(key, LeftPlaceholder(key, PopulateMode.Unknown));
                }
                if (0 != (Ops.RightNullModified & insertRule))
                {
                    rightInsert = Tuple.Create(key, RightPlaceholder(key, PopulateMode.NullModified));
                }
                else if (0 != (Ops.RightNullPreserve & insertRule))
                {
                    rightInsert = Tuple.Create(key, RightPlaceholder(key, PopulateMode.NullPreserve));
                }
                else if (0 != (Ops.RightUnknown & insertRule))
                {
                    rightInsert = Tuple.Create(key, RightPlaceholder(key, PopulateMode.Unknown));
                }

                if (0 != (Ops.RightNullModified & deleteRule))
                {
                    rightDelete = Tuple.Create(key, RightPlaceholder(key, PopulateMode.NullModified));
                }
                else if (0 != (Ops.RightNullPreserve & deleteRule))
                {
                    rightDelete = Tuple.Create(key, RightPlaceholder(key, PopulateMode.NullPreserve));
                }
                else if (0 != (Ops.RightUnknown & deleteRule))
                {
                    rightDelete = Tuple.Create(key, RightPlaceholder(key, PopulateMode.Unknown));
                }

                // Populate elements in join output
                if (null != leftInsert
                    && null != rightInsert)
                {
                    result.Inserted.Add(CreateResultTuple(leftInsert, rightInsert, result));
                }
                if (null != leftDelete
                    && null != rightDelete)
                {
                    result.Deleted.Add(CreateResultTuple(leftDelete, rightDelete, result));
                }
            }

            // <summary>
            // Produce a tuple containing joined rows.
            // </summary>
            // <param name="left"> Left row. </param>
            // <param name="right"> Right row. </param>
            // <param name="result"> Result change node; used for type information. </param>
            // <returns> Result of joining the input rows. </returns>
            private PropagatorResult CreateResultTuple(
                Tuple<CompositeKey, PropagatorResult> left, Tuple<CompositeKey, PropagatorResult> right, ChangeNode result)
            {
                // using ref compare to avoid triggering value based
                var leftKey = left.Item1;
                var rightKey = right.Item1;
                Dictionary<PropagatorResult, PropagatorResult> map = null;
                if (!ReferenceEquals(null, leftKey)
                    &&
                    !ReferenceEquals(null, rightKey)
                    &&
                    !ReferenceEquals(leftKey, rightKey))
                {
                    // Merge key values from the left and the right (since they're equal, there's a possibility we'll
                    // project values only from the left or the right hand side and lose important context.)
                    var mergedKey = leftKey.Merge(m_parent.m_updateTranslator.KeyManager, rightKey);
                    // create a dictionary so that we can replace key values with merged key values (carrying context
                    // from both sides)
                    map = new Dictionary<PropagatorResult, PropagatorResult>();
                    for (var i = 0; i < leftKey.KeyComponents.Length; i++)
                    {
                        map[leftKey.KeyComponents[i]] = mergedKey.KeyComponents[i];
                        map[rightKey.KeyComponents[i]] = mergedKey.KeyComponents[i];
                    }
                }

                var joinRecordValues = new PropagatorResult[2];
                joinRecordValues[0] = left.Item2;
                joinRecordValues[1] = right.Item2;
                var join = PropagatorResult.CreateStructuralValue(joinRecordValues, (StructuralType)result.ElementType.EdmType, false);

                // replace with merged key values as appropriate
                if (null != map)
                {
                    PropagatorResult replacement;
                    join = join.Replace(original => map.TryGetValue(original, out replacement) ? replacement : original);
                }

                return join;
            }

            // <summary>
            // Constructs a new placeholder record for the left hand side of the join. Values taken
            // from the join key are injected into the record.
            // </summary>
            // <param name="key"> Key producing the left hand side. </param>
            // <param name="mode"> Mode used to populate the placeholder </param>
            // <returns>
            // Record corresponding to the type of the left input to the join. Each value in the record is flagged as
            // <see
            //     cref="PropagatorFlags.Unknown" />
            // except when it is a component of the key.
            // </returns>
            private PropagatorResult LeftPlaceholder(CompositeKey key, PopulateMode mode)
            {
                return PlaceholderPopulator.Populate(m_left.Placeholder, key, m_leftPlaceholderKey, mode);
            }

            // <summary>
            // See <see cref="LeftPlaceholder" />
            // </summary>
            private PropagatorResult RightPlaceholder(CompositeKey key, PopulateMode mode)
            {
                return PlaceholderPopulator.Populate(m_right.Placeholder, key, m_rightPlaceholderKey, mode);
            }

            // <summary>
            // Produces a hash table of all instances and processes join keys, adding them to the list
            // of keys handled by this node.
            // </summary>
            // <param name="instances"> List of instances (whether delete or insert) for this node. </param>
            // <param name="keySelectors"> Selectors for key components. </param>
            // <returns> A map from join keys to instances. </returns>
            private JoinDictionary ProcessKeys(IEnumerable<PropagatorResult> instances, ReadOnlyCollection<DbExpression> keySelectors)
            {
                // Dictionary uses the composite key on both sides. This is because the composite key, in addition
                // to supporting comparison, maintains some context information (e.g., source of a value in the
                // state manager).
                var hash = new JoinDictionary(m_parent.UpdateTranslator.KeyComparer);

                foreach (var instance in instances)
                {
                    var key = ExtractKey(instance, keySelectors);
                    hash[key] = Tuple.Create(key, instance);
                }

                return hash;
            }

            // extracts key values from row expression
            private static CompositeKey ExtractKey(
                PropagatorResult change, ReadOnlyCollection<DbExpression> keySelectors)
            {
                DebugCheck.NotNull(change);
                DebugCheck.NotNull(keySelectors);

                var keyValues = new PropagatorResult[keySelectors.Count];
                for (var i = 0; i < keySelectors.Count; i++)
                {
                    var constant = Evaluator.Evaluate(keySelectors[i], change);
                    keyValues[i] = constant;
                }
                return new CompositeKey(keyValues);
            }

            // <summary>
            // Flags indicating which change elements are available (0-4) and propagation
            // rules (0, 5-512)
            // </summary>
            [Flags]
            private enum Ops : uint
            {
                Nothing = 0,
                LeftInsert = 1,
                LeftDelete = 2,
                RightInsert = 4,
                RightDelete = 8,
                LeftUnknown = 32,
                RightNullModified = 128,
                RightNullPreserve = 256,
                RightUnknown = 512,
                LeftUpdate = LeftInsert | LeftDelete,
                RightUpdate = RightInsert | RightDelete,
                Unsupported = 4096,

                #region Propagation rule descriptions

                LeftInsertJoinRightInsert = LeftInsert | RightInsert,
                LeftDeleteJoinRightDelete = LeftDelete | RightDelete,
                LeftInsertNullModifiedExtended = LeftInsert | RightNullModified,
                LeftInsertNullPreserveExtended = LeftInsert | RightNullPreserve,
                LeftInsertUnknownExtended = LeftInsert | RightUnknown,
                LeftDeleteNullModifiedExtended = LeftDelete | RightNullModified,
                LeftDeleteNullPreserveExtended = LeftDelete | RightNullPreserve,
                LeftDeleteUnknownExtended = LeftDelete | RightUnknown,
                LeftUnknownNullModifiedExtended = LeftUnknown | RightNullModified,
                LeftUnknownNullPreserveExtended = LeftUnknown | RightNullPreserve,
                RightInsertUnknownExtended = LeftUnknown | RightInsert,
                RightDeleteUnknownExtended = LeftUnknown | RightDelete,

                #endregion
            }
        }
    }
}
