// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration.Validation
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Mapping.ViewGeneration.Structures;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Text;
    using BasicSchemaConstraints = SchemaConstraints<BasicKeyConstraint>;

    // This class represents a relation signature that lists all scalar
    // slots for the join tree in cell query (before projection)
    internal class BasicCellRelation : CellRelation
    {
        // effects: Creates a basic cell relation for query
        internal BasicCellRelation(
            CellQuery cellQuery, ViewCellRelation viewCellRelation,
            IEnumerable<MemberProjectedSlot> slots)
            : base(viewCellRelation.CellNumber)
        {
            m_cellQuery = cellQuery;
            m_slots = new List<MemberProjectedSlot>(slots);
            Debug.Assert(m_slots.Count > 0, "Cell relation with not even an exent?");
            m_viewCellRelation = viewCellRelation;
        }

        private readonly CellQuery m_cellQuery;
        private readonly List<MemberProjectedSlot> m_slots;
        private readonly ViewCellRelation m_viewCellRelation; // The viewcellrelation
        // corresponding to this basiccellrelation

        internal ViewCellRelation ViewCellRelation
        {
            get { return m_viewCellRelation; }
        }

        // effects: Modifies constraints to contain the key constraints that
        // are present in this relation
        internal void PopulateKeyConstraints(BasicSchemaConstraints constraints)
        {
            Debug.Assert(this == m_cellQuery.BasicCellRelation, "Cellquery does not point to the correct BasicCellRelation?");
            Debug.Assert(
                m_cellQuery.Extent is EntitySet || m_cellQuery.Extent is AssociationSet,
                "Top level extents handled is currently entityset or association set");
            if (m_cellQuery.Extent is EntitySet)
            {
                PopulateKeyConstraintsForEntitySet(constraints);
            }
            else
            {
                PopulateKeyConstraintsForRelationshipSet(constraints);
            }
        }

        // requires: this to correspond to a cell relation for an entityset (m_cellQuery.Extent)
        // effects: Adds any key constraints present in this to constraints
        private void PopulateKeyConstraintsForEntitySet(BasicSchemaConstraints constraints)
        {
            var prefix = new MemberPath(m_cellQuery.Extent);
            var entityType = (EntityType)m_cellQuery.Extent.ElementType;

            // Get all the keys for the entity type and create the key constraints
            var keys = ExtentKey.GetKeysForEntityType(prefix, entityType);
            AddKeyConstraints(keys, constraints);
        }

        // requires: this to correspond to a cell relation for an association set (m_cellQuery.Extent)
        // effects: Adds any key constraints present in this relation in
        // constraints
        private void PopulateKeyConstraintsForRelationshipSet(BasicSchemaConstraints constraints)
        {
            var relationshipSet = m_cellQuery.Extent as AssociationSet;
            // Gather all members of all keys
            // CHANGE_ADYA_FEATURE_KEYS: assume that an Entity has exactly one key. Otherwise we
            // have to take a cross-product of all keys

            // Keep track of all the key members for the association in a set
            // so that if no end corresponds to a key, we use all the members
            // to form the key
            var associationKeyMembers = new Set<MemberPath>(MemberPath.EqualityComparer);
            var hasAnEndThatFormsKey = false;

            // Determine the keys of each end. If the end forms a key, add it
            // as a key to the set

            foreach (var end in relationshipSet.AssociationSetEnds)
            {
                var endMember = end.CorrespondingAssociationEndMember;

                var prefix = new MemberPath(relationshipSet, endMember);
                var keys = ExtentKey.GetKeysForEntityType(prefix, end.EntitySet.ElementType);
                Debug.Assert(keys.Count > 0, "No keys for entity?");
                Debug.Assert(keys.Count == 1, "Currently, we only support primary keys");

                if (MetadataHelper.DoesEndFormKey(relationshipSet, endMember))
                {
                    // This end has is a key end
                    AddKeyConstraints(keys, constraints);
                    hasAnEndThatFormsKey = true;
                }
                // Add the members of the (only) key to associationKey
                associationKeyMembers.AddRange(keys[0].KeyFields);
            }
            // If an end forms a key then that key implies the full key
            if (false == hasAnEndThatFormsKey)
            {
                // No end is a key -- take all the end members and make a key
                // based on that
                var key = new ExtentKey(associationKeyMembers);
                var keys = new[] { key };
                AddKeyConstraints(keys, constraints);
            }
        }

        // effects: Given keys for this relation, adds one key constraint for
        // each key present in keys
        private void AddKeyConstraints(IEnumerable<ExtentKey> keys, BasicSchemaConstraints constraints)
        {
            foreach (var key in keys)
            {
                // If the key is being projected, only then do we add the key constraint

                var keySlots = MemberProjectedSlot.GetSlots(m_slots, key.KeyFields);
                if (keySlots != null)
                {
                    var keyConstraint = new BasicKeyConstraint(this, keySlots);
                    constraints.Add(keyConstraint);
                }
            }
        }

        protected override int GetHash()
        {
            // Note: Using CLR-Hashcode
            return m_cellQuery.GetHashCode();
            // We need not hash the slots, etc - cellQuery should give us enough
            // differentiation and land the relation into the same bucket
        }

        internal override void ToCompactString(StringBuilder builder)
        {
            builder.Append("BasicRel: ");
            // Just print the extent name from slot 0
            StringUtil.FormatStringBuilder(builder, "{0}", m_slots[0]);
        }
    }
}
