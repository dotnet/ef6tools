// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration.Structures
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
    using System.Data.Entity.Core.Mapping.ViewGeneration.CqlGeneration;
    using System.Diagnostics;
    using System.Text;

    // <summary>
    // Wraps from0, from1, etc. boolean fields that identify the source of tuples (# of respective cell query) in the view statements.
    // </summary>
    internal class CellIdBoolean : TrueFalseLiteral
    {
        // <summary>
        // Creates a boolean expression for the variable name specified by <paramref name="index" />, e.g., 0 results in from0, 1 into from1.
        // </summary>
        internal CellIdBoolean(CqlIdentifiers identifiers, int index)
        {
            Debug.Assert(index >= 0);
            m_index = index;
            m_slotName = identifiers.GetFromVariable(index);
        }

        // <summary>
        // e.g., from0, from1.
        // </summary>
        private readonly int m_index;

        private readonly string m_slotName;

        // <summary>
        // Returns the slotName corresponding to this, ie., _from0 etc.
        // </summary>
        internal string SlotName
        {
            get { return m_slotName; }
        }

        internal override StringBuilder AsEsql(StringBuilder builder, string blockAlias, bool skipIsNotNull)
        {
            // Get e.g., T2._from1 using the table alias
            var qualifiedName = CqlWriter.GetQualifiedName(blockAlias, SlotName);
            builder.Append(qualifiedName);
            return builder;
        }

        internal override DbExpression AsCqt(DbExpression row, bool skipIsNotNull)
        {
            // Get e.g., row._from1
            return row.Property(SlotName);
        }

        internal override StringBuilder AsUserString(StringBuilder builder, string blockAlias, bool skipIsNotNull)
        {
            return AsEsql(builder, blockAlias, skipIsNotNull);
        }

        internal override StringBuilder AsNegatedUserString(StringBuilder builder, string blockAlias, bool skipIsNotNull)
        {
            builder.Append("NOT(");
            builder = AsUserString(builder, blockAlias, skipIsNotNull);
            builder.Append(")");
            return builder;
        }

        internal override void GetRequiredSlots(MemberProjectionIndex projectedSlotMap, bool[] requiredSlots)
        {
            // The slot corresponding to from1, etc
            var numBoolSlots = requiredSlots.Length - projectedSlotMap.Count;
            var slotNum = projectedSlotMap.BoolIndexToSlot(m_index, numBoolSlots);
            requiredSlots[slotNum] = true;
        }

        protected override bool IsEqualTo(BoolLiteral right)
        {
            var rightBoolean = right as CellIdBoolean;
            if (rightBoolean == null)
            {
                return false;
            }
            return m_index == rightBoolean.m_index;
        }

        public override int GetHashCode()
        {
            return m_index.GetHashCode();
        }

        internal override BoolLiteral RemapBool(Dictionary<MemberPath, MemberPath> remap)
        {
            return this;
        }

        internal override void ToCompactString(StringBuilder builder)
        {
            builder.Append(SlotName);
        }
    }
}
