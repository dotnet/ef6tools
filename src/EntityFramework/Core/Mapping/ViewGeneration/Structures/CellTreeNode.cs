// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration.Structures
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Mapping.ViewGeneration.CqlGeneration;
    using System.Data.Entity.Core.Mapping.ViewGeneration.QueryRewriting;
    using System.Linq;
    using System.Text;

    // This class represents a node in the update or query mapping view tree
    // (of course, the root node represents the full view)
    // Each node represents an expression of the form:
    // SELECT <Attributes> FROM <Expression> WHERE <Clause>
    // The WHERE clause is of the form X1 OR X2 OR ... where each Xi is a multiconstant
    internal abstract partial class CellTreeNode : InternalBase
    {
        // effects: Creates a cell tree node with a reference to projectedSlotMap for
        // deciphering the fields in this
        protected CellTreeNode(ViewgenContext context)
        {
            m_viewgenContext = context;
        }

        // effects: returns a copy of the tree below node
        internal CellTreeNode MakeCopy()
        {
            var visitor = new DefaultCellTreeVisitor<bool>();
            var result = Accept(visitor, true);
            return result;
        }

        private readonly ViewgenContext m_viewgenContext;

        // effects: Returns the operation being performed by this node
        internal abstract CellTreeOpType OpType { get; }

        // effects: Returns the right domain map associated with this celltreenode 
        internal abstract MemberDomainMap RightDomainMap { get; }

        internal abstract FragmentQuery LeftFragmentQuery { get; }

        internal abstract FragmentQuery RightFragmentQuery { get; }

        internal bool IsEmptyRightFragmentQuery
        {
            get { return !m_viewgenContext.RightFragmentQP.IsSatisfiable(RightFragmentQuery); }
        }

        // effects: Returns the attributes available/projected from this node
        internal abstract Set<MemberPath> Attributes { get; }

        // effects: Returns the children of this node
        internal abstract List<CellTreeNode> Children { get; }

        // effects: Returns the number of slots projected from this node
        internal abstract int NumProjectedSlots { get; }

        // effects: Returns the number of boolean slots in this node
        internal abstract int NumBoolSlots { get; }

        internal MemberProjectionIndex ProjectedSlotMap
        {
            get { return m_viewgenContext.MemberMaps.ProjectedSlotMap; }
        }

        internal ViewgenContext ViewgenContext
        {
            get { return m_viewgenContext; }
        }

        // effects: Given a leaf cell node and the slots required by the parent, returns
        // a CqlBlock corresponding to the tree rooted at this
        internal abstract CqlBlock ToCqlBlock(
            bool[] requiredSlots, CqlIdentifiers identifiers, ref int blockAliasNum,
            ref List<WithRelationship> withRelationships);

        // Effects: Returns true if slot at slot number "slot" is projected
        // by some node in tree rooted at this
        internal abstract bool IsProjectedSlot(int slot);

        // Standard accept method for visitor pattern. TOutput is the return
        // type for visitor methods.
        internal abstract TOutput Accept<TInput, TOutput>(CellTreeVisitor<TInput, TOutput> visitor, TInput param);
        internal abstract TOutput Accept<TInput, TOutput>(SimpleCellTreeVisitor<TInput, TOutput> visitor, TInput param);

        // effects: Given a cell tree node , removes unnecessary
        // "nesting" that occurs in the tree -- an unnecessary nesting
        // occurs when a node has exactly one child.
        internal CellTreeNode Flatten()
        {
            return FlatteningVisitor.Flatten(this);
        }

        // effects: Gets all the leaves in this
        internal List<LeftCellWrapper> GetLeaves()
        {
            return GetLeafNodes().Select(leafNode => leafNode.LeftCellWrapper).ToList();
        }

        // effects: Gets all the leaves in this
        internal IEnumerable<LeafCellTreeNode> GetLeafNodes()
        {
            return LeafVisitor.GetLeaves(this);
        }

        // effects: Like Flatten, flattens the tree and then collapses
        // associative operators, e.g., (A IJ B) IJ C is changed to A IJ B IJ C
        internal CellTreeNode AssociativeFlatten()
        {
            return AssociativeOpFlatteningVisitor.Flatten(this);
        }

        // effects: Returns true iff the Op (e.g., IJ) is associative, i.e.,
        // A OP (B OP C) is the same as (A OP B) OP C or A OP B OP C
        internal static bool IsAssociativeOp(CellTreeOpType opType)
        {
            // This is not true for LOJ and LASJ            
            return opType == CellTreeOpType.IJ || opType == CellTreeOpType.Union ||
                   opType == CellTreeOpType.FOJ;
        }

        // effects: Returns an array of booleans where bool[i] is set to true
        // iff some node in the tree rooted at node projects that slot
        internal bool[] GetProjectedSlots()
        {
            // Gets the information on the normal and the boolean slots
            var totalSlots = ProjectedSlotMap.Count + NumBoolSlots;
            var slots = new bool[totalSlots];
            for (var i = 0; i < totalSlots; i++)
            {
                slots[i] = IsProjectedSlot(i);
            }
            return slots;
        }

        // effects: Given a slot number, slotNum, returns the output member path
        // that this slot contributes/corresponds to in the extent view. If
        // the slot corresponds to one of the boolean variables, returns null
        protected MemberPath GetMemberPath(int slotNum)
        {
            return ProjectedSlotMap.GetMemberPath(slotNum, NumBoolSlots);
        }

        // effects: Given the index of a boolean variable (e.g., of from1),
        // returns the slot number for that boolean in this
        protected int BoolIndexToSlot(int boolIndex)
        {
            // Booleans appear after the regular slot
            return ProjectedSlotMap.BoolIndexToSlot(boolIndex, NumBoolSlots);
        }

        // effects: Given a slotNum corresponding to a boolean slot, returns
        // the cel number that the cell corresponds to
        protected int SlotToBoolIndex(int slotNum)
        {
            return ProjectedSlotMap.SlotToBoolIndex(slotNum, NumBoolSlots);
        }

        // effects: Returns true if slotNum corresponds to a key slot in the
        // output extent view
        protected bool IsKeySlot(int slotNum)
        {
            return ProjectedSlotMap.IsKeySlot(slotNum, NumBoolSlots);
        }

        // effects: Returns true if slotNum corresponds to a bool slot and
        // not a regular field
        protected bool IsBoolSlot(int slotNum)
        {
            return ProjectedSlotMap.IsBoolSlot(slotNum, NumBoolSlots);
        }

        // effects: Returns the slot numbers corresponding to the key fields
        // in the m_projectedSlotMap
        protected IEnumerable<int> KeySlots
        {
            get
            {
                var numMembers = ProjectedSlotMap.Count;
                for (var slotNum = 0; slotNum < numMembers; slotNum++)
                {
                    if (IsKeySlot(slotNum))
                    {
                        yield return slotNum;
                    }
                }
            }
        }

        // effects: Modifies builder to contain a Cql query corresponding to
        // the tree rooted at this
        internal override void ToFullString(StringBuilder builder)
        {
            var blockAliasNum = 0;
            // Get the required slots, get the block and then get the string
            var requiredSlots = GetProjectedSlots();
            // Using empty identifiers over here since we do not use this for the actual CqlGeneration
            var identifiers = new CqlIdentifiers();
            var withRelationships = new List<WithRelationship>();
            var block = ToCqlBlock(requiredSlots, identifiers, ref blockAliasNum, ref withRelationships);
            block.AsEsql(builder, false, 1);
        }
    }
}
