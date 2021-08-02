// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration.Structures
{
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Mapping.ViewGeneration.CqlGeneration;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Text;

    // <summary>
    // A constant that can be projected in a cell query.
    // </summary>
    internal sealed class ConstantProjectedSlot : ProjectedSlot
    {
        // <summary>
        // Creates a slot with constant value being <paramref name="value" />.
        // </summary>
        internal ConstantProjectedSlot(Constant value)
        {
            DebugCheck.NotNull(value);
            Debug.Assert(value.IsNotNull() == false, "Cannot store NotNull in a slot - NotNull is only for conditions");
            m_constant = value;
        }

        // <summary>
        // The actual value.
        // </summary>
        private readonly Constant m_constant;

        // <summary>
        // Returns the value stored in this constant.
        // </summary>
        internal Constant CellConstant
        {
            get { return m_constant; }
        }

        internal override ProjectedSlot DeepQualify(CqlBlock block)
        {
            return this; // Nothing to create
        }

        internal override StringBuilder AsEsql(StringBuilder builder, MemberPath outputMember, string blockAlias, int indentLevel)
        {
            return m_constant.AsEsql(builder, outputMember, blockAlias);
        }

        internal override DbExpression AsCqt(DbExpression row, MemberPath outputMember)
        {
            return m_constant.AsCqt(row, outputMember);
        }

        protected override bool IsEqualTo(ProjectedSlot right)
        {
            var rightSlot = right as ConstantProjectedSlot;
            if (rightSlot == null)
            {
                return false;
            }
            return Constant.EqualityComparer.Equals(m_constant, rightSlot.m_constant);
        }

        protected override int GetHash()
        {
            return Constant.EqualityComparer.GetHashCode(m_constant);
        }

        internal override void ToCompactString(StringBuilder builder)
        {
            m_constant.ToCompactString(builder);
        }
    }
}
