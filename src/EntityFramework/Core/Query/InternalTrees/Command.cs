// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Query.PlanCompiler;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    // <summary>
    // The Command object encapsulates all information relating to a single command.
    // It includes the expression tree in question, as well as the parameters to the
    // command.
    // Additionally, the Command class serves as a factory for building up different
    // nodes and Ops. Every node in the tree has a unique id, and this is enforced by
    // the node factory methods
    // </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal class Command
    {
        #region private state

        private readonly Dictionary<string, ParameterVar> m_parameterMap;
        private readonly List<Var> m_vars;
        private readonly List<Table> m_tables;
        private readonly MetadataWorkspace m_metadataWorkspace;
        private readonly TypeUsage m_boolType;
        private readonly TypeUsage m_intType;
        private readonly TypeUsage m_stringType;
        private readonly ConstantPredicateOp m_trueOp;
        private readonly ConstantPredicateOp m_falseOp;
        private readonly NodeInfoVisitor m_nodeInfoVisitor;
        private readonly KeyPullup m_keyPullupVisitor;
        private int m_nextNodeId;
        private int m_nextBranchDiscriminatorValue = 1000;

        private bool m_disableVarVecEnumCaching;
        private readonly Stack<VarVec.VarVecEnumerator> m_freeVarVecEnumerators;
        private readonly Stack<VarVec> m_freeVarVecs;

        // set of referenced rel properties in this query
        private readonly HashSet<RelProperty> m_referencedRelProperties;

        #endregion

        #region constructors

        // <summary>
        // Creates a new command
        // </summary>
        internal Command(MetadataWorkspace metadataWorkspace)
        {
            m_parameterMap = new Dictionary<string, ParameterVar>();
            m_vars = new List<Var>();
            m_tables = new List<Table>();
            m_metadataWorkspace = metadataWorkspace;
            if (!TryGetPrimitiveType(PrimitiveTypeKind.Boolean, out m_boolType))
            {
                throw new ProviderIncompatibleException(Strings.Cqt_General_NoProviderBooleanType);
            }
            if (!TryGetPrimitiveType(PrimitiveTypeKind.Int32, out m_intType))
            {
                throw new ProviderIncompatibleException(Strings.Cqt_General_NoProviderIntegerType);
            }
            if (!TryGetPrimitiveType(PrimitiveTypeKind.String, out m_stringType))
            {
                throw new ProviderIncompatibleException(Strings.Cqt_General_NoProviderStringType);
            }
            m_trueOp = new ConstantPredicateOp(m_boolType, true);
            m_falseOp = new ConstantPredicateOp(m_boolType, false);
            m_nodeInfoVisitor = new NodeInfoVisitor(this);
            m_keyPullupVisitor = new KeyPullup(this);

            // FreeLists
            m_freeVarVecEnumerators = new Stack<VarVec.VarVecEnumerator>();
            m_freeVarVecs = new Stack<VarVec>();

            m_referencedRelProperties = new HashSet<RelProperty>();
        }

        // <summary>
        // For mocking.
        // </summary>
        internal Command()
        {
        }

        #endregion

        #region public methods

        // <summary>
        // Gets the metadata workspace associated with this command
        // </summary>
        internal virtual MetadataWorkspace MetadataWorkspace
        {
            get { return m_metadataWorkspace; }
        }

        // <summary>
        // Gets/sets the root node of the query
        // </summary>
        internal virtual Node Root { get; set; }

        internal virtual void DisableVarVecEnumCaching()
        {
            m_disableVarVecEnumCaching = true;
        }

        // <summary>
        // Returns the next value for a UnionAll BranchDiscriminator.
        // </summary>
        internal virtual int NextBranchDiscriminatorValue
        {
            get { return m_nextBranchDiscriminatorValue++; }
        }

        // <summary>
        // Returns the next value for a node id, without incrementing it.
        // </summary>
        internal virtual int NextNodeId
        {
            get { return m_nextNodeId; }
        }

        #region Metadata Helpers

        // <summary>
        // Helper routine to get the metadata representation for the bool type
        // </summary>
        internal virtual TypeUsage BooleanType
        {
            get { return m_boolType; }
        }

        // <summary>
        // Helper routine to get the metadata representation of the int type
        // </summary>
        internal virtual TypeUsage IntegerType
        {
            get { return m_intType; }
        }

        // <summary>
        // Helper routine to get the metadata representation of the string type
        // </summary>
        internal virtual TypeUsage StringType
        {
            get { return m_stringType; }
        }

        // <summary>
        // Get the primitive type by primitive type kind
        // </summary>
        // <param name="modelType"> EdmMetadata.PrimitiveTypeKind of the primitive type </param>
        // <param name="type"> A TypeUsage that represents the specified primitive type </param>
        // <returns>
        // <c>True</c> if the specified primitive type could be retrieved; otherwise <c>false</c> .
        // </returns>
        private static bool TryGetPrimitiveType(PrimitiveTypeKind modelType, out TypeUsage type)
        {
            type = null;

            if (modelType == PrimitiveTypeKind.String)
            {
                type = TypeUsage.CreateStringTypeUsage(
                    MetadataWorkspace.GetModelPrimitiveType(modelType),
                    false /*unicode*/,
                    false /*fixed*/);
            }
            else
            {
                type = MetadataWorkspace.GetCanonicalModelTypeUsage(modelType);
            }

            return (null != type);
        }

        #endregion

        #region VarVec Creation

        // <summary>
        // VarVec constructor
        // </summary>
        // <returns> A new, empty, VarVec </returns>
        internal virtual VarVec CreateVarVec()
        {
            VarVec vec;
            if (m_freeVarVecs.Count == 0)
            {
                vec = new VarVec(this);
            }
            else
            {
                vec = m_freeVarVecs.Pop();
                vec.Clear();
            }
            return vec;
        }

        // <summary>
        // Create a VarVec with a single Var
        // </summary>
        internal virtual VarVec CreateVarVec(Var v)
        {
            var varset = CreateVarVec();
            varset.Set(v);
            return varset;
        }

        // <summary>
        // Create a VarVec with the set of specified vars
        // </summary>
        internal virtual VarVec CreateVarVec(IEnumerable<Var> v)
        {
            var vec = CreateVarVec();
            vec.InitFrom(v);
            return vec;
        }

        // <summary>
        // Create a new VarVec from the input VarVec
        // </summary>
        internal virtual VarVec CreateVarVec(VarVec v)
        {
            var vec = CreateVarVec();
            vec.InitFrom(v);
            return vec;
        }

        // <summary>
        // Release a VarVec to the freelist
        // </summary>
        internal virtual void ReleaseVarVec(VarVec vec)
        {
            m_freeVarVecs.Push(vec);
        }

        #endregion

        #region VarVecEnumerator

        // <summary>
        // Create a new enumerator for a VarVec; use a free one if its
        // available; otherwise, create a new one
        // </summary>
        internal virtual VarVec.VarVecEnumerator GetVarVecEnumerator(VarVec vec)
        {
            VarVec.VarVecEnumerator enumerator;

            if (m_disableVarVecEnumCaching ||
                m_freeVarVecEnumerators.Count == 0)
            {
                enumerator = new VarVec.VarVecEnumerator(vec);
            }
            else
            {
                enumerator = m_freeVarVecEnumerators.Pop();
                enumerator.Init(vec);
            }
            return enumerator;
        }

        // <summary>
        // Release an enumerator; keep it in a local stack for future use
        // </summary>
        internal virtual void ReleaseVarVecEnumerator(VarVec.VarVecEnumerator enumerator)
        {
            if (!m_disableVarVecEnumCaching)
            {
                m_freeVarVecEnumerators.Push(enumerator);
            }
        }

        #endregion

        #region VarList

        // <summary>
        // Create an ordered list of Vars - initially empty
        // </summary>
        internal static VarList CreateVarList()
        {
            return new VarList();
        }

        // <summary>
        // Create an ordered list of Vars
        // </summary>
        internal static VarList CreateVarList(IEnumerable<Var> vars)
        {
            return new VarList(vars);
        }

        #endregion

        #region VarMap

        #endregion

        #region Table Helpers

        private int NewTableId()
        {
            return m_tables.Count;
        }

        // <summary>
        // Create a table whose element type is "elementType"
        // </summary>
        // <param name="elementType"> type of each element (row) of the table </param>
        // <returns> a table definition object </returns>
        internal static TableMD CreateTableDefinition(TypeUsage elementType)
        {
            return new TableMD(elementType, null);
        }

        // <summary>
        // Creates a new table definition based on an extent. The element type
        // of the extent manifests as the single column of the table
        // </summary>
        // <param name="extent"> the metadata extent </param>
        // <returns> A new TableMD instance based on the extent </returns>
        internal static TableMD CreateTableDefinition(EntitySetBase extent)
        {
            return new TableMD(TypeUsage.Create(extent.ElementType), extent);
        }

        // <summary>
        // Create a "flat" table definition object (ie) the table has one column
        // for each property of the specified row type
        // </summary>
        // <param name="type"> the shape of each row of the table </param>
        // <returns> the table definition </returns>
        internal virtual TableMD CreateFlatTableDefinition(RowType type)
        {
            return CreateFlatTableDefinition(type.Properties, new List<EdmMember>(), null);
        }

        // <summary>
        // Create a "flat" table defintion. The table has one column for each property
        // specified, and the key columns of the table are those specified in the
        // keyMembers parameter
        // </summary>
        // <param name="properties"> list of columns for the table </param>
        // <param name="keyMembers"> the key columns (if any) </param>
        // <param name="entitySet"> (OPTIONAL) entityset corresponding to this table </param>
        internal virtual TableMD CreateFlatTableDefinition(
            IEnumerable<EdmProperty> properties, IEnumerable<EdmMember> keyMembers, EntitySetBase entitySet)
        {
            return new TableMD(properties, keyMembers, entitySet);
        }

        // <summary>
        // Creates a new table instance
        // </summary>
        // <param name="tableMetadata"> table metadata </param>
        // <returns> A new Table instance with columns as defined in the specified metadata </returns>
        internal virtual Table CreateTableInstance(TableMD tableMetadata)
        {
            var t = new Table(this, tableMetadata, NewTableId());
            m_tables.Add(t);
            return t;
        }

        #endregion

        #region Var Access

        // <summary>
        // All vars in the query
        // </summary>
        internal virtual IEnumerable<Var> Vars
        {
            get { return m_vars.Where(v => v.VarType != VarType.NotValid); }
        }

        // <summary>
        // Access an existing variable in the query (by its id)
        // </summary>
        // <param name="id"> The ID of the variable to retrieve </param>
        // <returns> The variable with the specified ID </returns>
        internal virtual Var GetVar(int id)
        {
            Debug.Assert(m_vars[id].VarType != VarType.NotValid, "The var has been replaced by a different var and is no longer valid.");

            return m_vars[id];
        }

        // <summary>
        // Gets the ParameterVar that corresponds to a given named parameter
        // </summary>
        // <param name="paramName"> The name of the parameter for which to retrieve the ParameterVar </param>
        // <returns> The ParameterVar that corresponds to the specified parameter </returns>
        internal virtual ParameterVar GetParameter(string paramName)
        {
            return m_parameterMap[paramName];
        }

        #endregion

        #region Var Creation

        private int NewVarId()
        {
            return m_vars.Count;
        }

        // <summary>
        // Creates a variable for a parameter in the query
        // </summary>
        // <param name="parameterName"> The name of the parameter for which to create the var </param>
        // <param name="parameterType"> The type of the parameter, and therefore the new var </param>
        // <returns> A new ParameterVar instance with the specified name and type </returns>
        internal virtual ParameterVar CreateParameterVar(
            string parameterName,
            TypeUsage parameterType)
        {
            if (m_parameterMap.ContainsKey(parameterName))
            {
                throw new ArgumentException(Strings.DuplicateParameterName(parameterName));
            }
            var v = new ParameterVar(NewVarId(), parameterType, parameterName);
            m_vars.Add(v);
            m_parameterMap[parameterName] = v;
            return v;
        }

        // <summary>
        // Creates a variable for the given parameter variable and replaces it in parameter map.
        // </summary>
        // <param name="oldVar"> Parameter variable that needs to replaced. </param>
        // <param name="generateReplacementType"> Delegate that generates the replacement parameter's type. </param>
        // <returns>
        // A new ParameterVar instance created of <paramref name="oldVar" /> .
        // </returns>
        // <remarks>
        // This method should be used only to replace external enum or strong spatial parameters with a counterpart whose
        // type is the underlying type of the enum type, or the union type contating the strong spatial type of the
        // <paramref
        //     name="oldVar" />
        // .
        // The operation invalidates the <paramref name="oldVar" />. After the operation has completed
        // the <paramref name="oldVar" />) is invalidated internally and should no longer be used.
        // </remarks>
        private ParameterVar ReplaceParameterVar(ParameterVar oldVar, Func<TypeUsage, TypeUsage> generateReplacementType)
        {
            DebugCheck.NotNull(oldVar);
            Debug.Assert(m_vars.Contains(oldVar));
            var v = new ParameterVar(NewVarId(), generateReplacementType(oldVar.Type), oldVar.ParameterName);
            m_parameterMap[oldVar.ParameterName] = v;
            m_vars.Add(v);
            return v;
        }

        // <summary>
        // Creates a variable for the given enum parameter variable and replaces it in parameter map.
        // </summary>
        // <param name="oldVar"> Enum parameter variable that needs to replaced. </param>
        // <returns>
        // A new ParameterVar instance created of <paramref name="oldVar" /> .
        // </returns>
        // <remarks>
        // This method should be used only to replace external enum parameter with a counterpart whose
        // type is the underlying type of the enum type of the <paramref name="oldVar" />.
        // The operation invalidates the <paramref name="oldVar" />. After the operation has completed
        // the <paramref name="oldVar" />) is invalidated internally and should no longer be used.
        // </remarks>
        internal virtual ParameterVar ReplaceEnumParameterVar(ParameterVar oldVar)
        {
            return ReplaceParameterVar(oldVar, t => TypeHelpers.CreateEnumUnderlyingTypeUsage(t));
        }

        // <summary>
        // Creates a variable for the given spatial parameter variable and replaces it in parameter map.
        // </summary>
        // <param name="oldVar"> Spatial parameter variable that needs to replaced. </param>
        // <returns>
        // A new ParameterVar instance created of <paramref name="oldVar" /> .
        // </returns>
        // <remarks>
        // This method should be used only to replace external strong spatial parameter with a counterpart whose
        // type is the appropriate union type for <paramref name="oldVar" />.
        // The operation invalidates the <paramref name="oldVar" />. After the operation has completed
        // the <paramref name="oldVar" />) is invalidated internally and should no longer be used.
        // </remarks>
        internal virtual ParameterVar ReplaceStrongSpatialParameterVar(ParameterVar oldVar)
        {
            return ReplaceParameterVar(oldVar, t => TypeHelpers.CreateSpatialUnionTypeUsage(t));
        }

        // <summary>
        // Creates a new var for a table column
        // </summary>
        // <param name="table"> The table instance that produces the column </param>
        // <param name="columnMD"> column metadata </param>
        // <returns> A new ColumnVar instance that references the specified column in the given table </returns>
        internal virtual ColumnVar CreateColumnVar(Table table, ColumnMD columnMD)
        {
            // create a new column var now
            var c = new ColumnVar(NewVarId(), table, columnMD);
            table.Columns.Add(c);
            m_vars.Add(c);
            return c;
        }

        // <summary>
        // Creates a computed var (ie) a variable that is computed by an expression
        // </summary>
        // <param name="type"> The type of the result produced by the expression that defines the variable </param>
        // <returns> A new ComputedVar instance with the specified result type </returns>
        internal virtual ComputedVar CreateComputedVar(TypeUsage type)
        {
            var v = new ComputedVar(NewVarId(), type);
            m_vars.Add(v);
            return v;
        }

        // <summary>
        // Creates a SetOp Var of
        // </summary>
        // <param name="type"> Datatype of the Var </param>
        // <returns> A new SetOp Var with the specified result type </returns>
        internal virtual SetOpVar CreateSetOpVar(TypeUsage type)
        {
            var v = new SetOpVar(NewVarId(), type);
            m_vars.Add(v);
            return v;
        }

        #endregion

        #region Node Creation

        //
        // The routines below help in node construction. All command tree nodes must go
        // through these routines. These routines help to stamp each node with a unique
        // id (the id is very helpful for debugging)
        //

        // <summary>
        // Creates a Node with zero children
        // </summary>
        // <param name="op"> The operator that the Node should reference </param>
        // <returns> A new Node with zero children that references the specified Op </returns>
        internal virtual Node CreateNode(Op op)
        {
            return CreateNode(op, new List<Node>());
        }

        // <summary>
        // Creates a node with a single child Node
        // </summary>
        // <param name="op"> The operator that the Node should reference </param>
        // <param name="arg1"> The single child Node </param>
        // <returns> A new Node with the specified child Node, that references the specified Op </returns>
        internal virtual Node CreateNode(Op op, Node arg1)
        {
            var l = new List<Node>();
            l.Add(arg1);
            return CreateNode(op, l);
        }

        // <summary>
        // Creates a node with two child Nodes
        // </summary>
        // <param name="op"> The operator that the Node should reference </param>
        // <param name="arg1"> The first child Node </param>
        // <param name="arg2"> the second child Node </param>
        // <returns> A new Node with the specified child Nodes, that references the specified Op </returns>
        internal virtual Node CreateNode(Op op, Node arg1, Node arg2)
        {
            var l = new List<Node>();
            l.Add(arg1);
            l.Add(arg2);
            return CreateNode(op, l);
        }

        // <summary>
        // Creates a node with 3 child Nodes
        // </summary>
        // <param name="op"> The operator that the Node should reference </param>
        // <param name="arg1"> The first child Node </param>
        // <param name="arg2"> The second child Node </param>
        // <param name="arg3"> The third child Node </param>
        // <returns> A new Node with the specified child Nodes, that references the specified Op </returns>
        internal virtual Node CreateNode(Op op, Node arg1, Node arg2, Node arg3)
        {
            var l = new List<Node>();
            l.Add(arg1);
            l.Add(arg2);
            l.Add(arg3);
            return CreateNode(op, l);
        }

        // <summary>
        // Create a Node with the specified list of child Nodes
        // </summary>
        // <param name="op"> The operator that the Node should reference </param>
        // <param name="args"> The list of child Nodes </param>
        // <returns> A new Node with the specified child nodes, that references the specified Op </returns>
        internal virtual Node CreateNode(Op op, IList<Node> args)
        {
            return new Node(m_nextNodeId++, op, new List<Node>(args));
        }

        // <summary>
        // Create a Node with the specified list of child Nodes
        // </summary>
        // <param name="op"> The operator that the Node should reference </param>
        // <param name="args"> The list of child Nodes </param>
        // <returns> A new Node with the specified child nodes, that references the specified Op </returns>
        internal virtual Node CreateNode(Op op, List<Node> args)
        {
            return new Node(m_nextNodeId++, op, args);
        }

        #endregion

        #region ScalarOps

        // <summary>
        // Creates a new ConstantOp
        // </summary>
        // <param name="type"> The type of the constant value </param>
        // <param name="value"> The constant value (may be null) </param>
        // <returns> A new ConstantOp with the specified type and value </returns>
        internal virtual ConstantBaseOp CreateConstantOp(TypeUsage type, object value)
        {
            // create a NullOp if necessary
            if (value == null)
            {
                return new NullOp(type);
            }
            // Identify "safe" constants - the only safe ones are boolean (and we should
            // probably include ints eventually)
            else if (TypeSemantics.IsBooleanType(type))
            {
                return new InternalConstantOp(type, value);
            }
            else
            {
                return new ConstantOp(type, value);
            }
        }

        // <summary>
        // Create an "internal" constantOp - only for use by the plan compiler to
        // represent internally generated constants.
        // User constants in the query should never get into this function
        // </summary>
        // <param name="type"> datatype of the constant </param>
        // <param name="value"> constant value </param>
        // <returns> a new "internal" constant op that represents the constant </returns>
        internal virtual InternalConstantOp CreateInternalConstantOp(TypeUsage type, object value)
        {
            return new InternalConstantOp(type, value);
        }

        // <summary>
        // An internal constant that serves as a null sentinel, i.e. it is only ever used
        // to be checked whether it is null
        // </summary>
        internal virtual NullSentinelOp CreateNullSentinelOp()
        {
            return new NullSentinelOp(IntegerType, 1);
        }

        // <summary>
        // An "internal" null constant
        // </summary>
        // <param name="type"> datatype of the null constant </param>
        // <returns> a new "internal" null constant op </returns>
        internal virtual NullOp CreateNullOp(TypeUsage type)
        {
            return new NullOp(type);
        }

        // <summary>
        // Create a constant predicateOp
        // </summary>
        // <param name="value"> value of the constant predicate </param>
        internal virtual ConstantPredicateOp CreateConstantPredicateOp(bool value)
        {
            return value ? m_trueOp : m_falseOp;
        }

        // <summary>
        // Create a constant predicate with value=true
        // </summary>
        internal virtual ConstantPredicateOp CreateTrueOp()
        {
            return m_trueOp;
        }

        // <summary>
        // Create a constant predicateOp with the value false
        // </summary>
        internal virtual ConstantPredicateOp CreateFalseOp()
        {
            return m_falseOp;
        }

        // <summary>
        // Creates a new FunctionOp
        // </summary>
        // <param name="function"> EdmFunction metadata that represents the function that is invoked by the Op </param>
        // <returns> A new FunctionOp that references the specified function metadata </returns>
        internal virtual FunctionOp CreateFunctionOp(EdmFunction function)
        {
            return new FunctionOp(function);
        }

        // <summary>
        // Creates a new TreatOp
        // </summary>
        // <param name="type"> Type metadata that specifies the type that the child of the treat node should be treated as </param>
        // <returns> A new TreatOp that references the specified type metadata </returns>
        internal virtual TreatOp CreateTreatOp(TypeUsage type)
        {
            return new TreatOp(type, false);
        }

        // <summary>
        // Create a "dummy" treatOp (i.e.) we can actually ignore the treatOp.
        // </summary>
        internal virtual TreatOp CreateFakeTreatOp(TypeUsage type)
        {
            return new TreatOp(type, true);
        }

        // <summary>
        // Creates a new IsOfOp, which tests if the argument is of the specified type or a promotable type
        // </summary>
        // <param name="isOfType"> Type metadata that specifies the type with which the type of the argument should be compared </param>
        // <returns> A new IsOfOp that references the specified type metadata </returns>
        internal virtual IsOfOp CreateIsOfOp(TypeUsage isOfType)
        {
            return new IsOfOp(isOfType, false /*only*/, m_boolType);
        }

        // <summary>
        // Creates a new IsOfOp, which tests if the argument is of the specified type (and only the specified type)
        // </summary>
        // <param name="isOfType"> Type metadata that specifies the type with which the type of the argument should be compared </param>
        // <returns> A new IsOfOp that references the specified type metadata </returns>
        internal virtual IsOfOp CreateIsOfOnlyOp(TypeUsage isOfType)
        {
            return new IsOfOp(isOfType, true /* "only" */, m_boolType);
        }

        // <summary>
        // Creates a new CastOp
        // </summary>
        // <param name="type"> Type metadata that represents the type to which the argument should be cast </param>
        // <returns> A new CastOp that references the specified type metadata </returns>
        internal virtual CastOp CreateCastOp(TypeUsage type)
        {
            return new CastOp(type);
        }

        // <summary>
        // Creates a new SoftCastOp and casts the input to the desired type.
        // The caller is expected to determine if the cast is necessary or not
        // </summary>
        // <param name="type"> Type metadata that represents the type to which the argument should be cast </param>
        // <returns> A new CastOp that references the specified type metadata </returns>
        internal virtual SoftCastOp CreateSoftCastOp(TypeUsage type)
        {
            return new SoftCastOp(type);
        }

        // <summary>
        // Creates a new ComparisonOp of the specified type
        // </summary>
        // <param name="opType"> An OpType that specifies one of the valid comparison OpTypes: EQ, GT, GE, NE, LT, LE </param>
        // <param name="useDatabaseNullSemantics"> Specifies whether database null comparison behavior is enabled. </param>
        // <returns> A new ComparisonOp of the specified comparison OpType </returns>
        internal virtual ComparisonOp CreateComparisonOp(OpType opType, bool useDatabaseNullSemantics = false)
        {
            return new ComparisonOp(opType, BooleanType) { UseDatabaseNullSemantics = useDatabaseNullSemantics };
        }

        // <summary>
        // Creates a new LikeOp
        // </summary>
        // <returns> The new LikeOp </returns>
        internal virtual LikeOp CreateLikeOp()
        {
            return new LikeOp(BooleanType);
        }

        // <summary>
        // Creates a new ConditionalOp of the specified type
        // </summary>
        // <param name="opType"> An OpType that specifies one of the valid condition operations: And, Or, Not, IsNull </param>
        // <returns> A new ConditionalOp with the specified conditional OpType </returns>
        internal virtual ConditionalOp CreateConditionalOp(OpType opType)
        {
            return new ConditionalOp(opType, BooleanType);
        }

        // <summary>
        // Creates a new CaseOp
        // </summary>
        // <param name="type"> The result type of the CaseOp </param>
        // <returns> A new CaseOp with the specified result type </returns>
        internal virtual CaseOp CreateCaseOp(TypeUsage type)
        {
            return new CaseOp(type);
        }

        // <summary>
        // Creates a new AggregateOp
        // </summary>
        // <param name="aggFunc"> EdmFunction metadata that specifies the aggregate function </param>
        // <param name="distinctAgg"> Indicates whether or not the aggregate is a distinct aggregate </param>
        // <returns> A new AggregateOp with the specified function metadata and distinct property </returns>
        internal virtual AggregateOp CreateAggregateOp(EdmFunction aggFunc, bool distinctAgg)
        {
            return new AggregateOp(aggFunc, distinctAgg);
        }

        // <summary>
        // Creates a named type constructor
        // </summary>
        // <param name="type"> Type metadata that specifies the type of the instance to construct </param>
        // <returns> A new NewInstanceOp with the specified result type </returns>
        internal virtual NewInstanceOp CreateNewInstanceOp(TypeUsage type)
        {
            return new NewInstanceOp(type);
        }

        // <summary>
        // Build out a new NewEntityOp constructing the entity <paramref name="type" /> scoped to the
        // <paramref
        //     name="entitySet" />
        // .
        // </summary>
        internal virtual NewEntityOp CreateScopedNewEntityOp(TypeUsage type, List<RelProperty> relProperties, EntitySet entitySet)
        {
            return new NewEntityOp(type, relProperties, true, entitySet);
        }

        // <summary>
        // Build out a new NewEntityOp constructing the uscoped entity <paramref name="type" />.
        // </summary>
        internal virtual NewEntityOp CreateNewEntityOp(TypeUsage type, List<RelProperty> relProperties)
        {
            return new NewEntityOp(type, relProperties, false, null);
        }

        // <summary>
        // Create a discriminated named type constructor
        // </summary>
        // <param name="type"> Type metadata that specifies the type of the instance to construct </param>
        // <param name="discriminatorMap"> Mapping information including discriminator values </param>
        // <param name="entitySet"> the entityset that this instance belongs to </param>
        // <param name="relProperties"> list of rel properties that have corresponding values </param>
        // <returns> A new DiscriminatedNewInstanceOp with the specified result type and discrimination behavior </returns>
        internal virtual DiscriminatedNewEntityOp CreateDiscriminatedNewEntityOp(
            TypeUsage type, ExplicitDiscriminatorMap discriminatorMap,
            EntitySet entitySet, List<RelProperty> relProperties)
        {
            return new DiscriminatedNewEntityOp(type, discriminatorMap, entitySet, relProperties);
        }

        // <summary>
        // Creates a multiset constructor
        // </summary>
        // <param name="type"> Type metadata that specifies the type of the multiset to construct </param>
        // <returns> A new NewMultiSetOp with the specified result type </returns>
        internal virtual NewMultisetOp CreateNewMultisetOp(TypeUsage type)
        {
            return new NewMultisetOp(type);
        }

        // <summary>
        // Creates a record constructor
        // </summary>
        // <param name="type"> Type metadata that specifies that record type to construct </param>
        // <returns> A new NewRecordOp with the specified result type </returns>
        internal virtual NewRecordOp CreateNewRecordOp(TypeUsage type)
        {
            return new NewRecordOp(type);
        }

        // <summary>
        // Creates a record constructor
        // </summary>
        // <param name="type"> Type metadata that specifies that record type to construct </param>
        // <returns> A new NewRecordOp with the specified result type </returns>
        internal virtual NewRecordOp CreateNewRecordOp(RowType type)
        {
            return new NewRecordOp(TypeUsage.Create(type));
        }

        // <summary>
        // A variant of the above method to create a NewRecordOp. An additional
        // argument - fields - is supplied, and the semantics is that only these fields
        // have any values specified as part of the Node. All other fields are
        // considered to be null.
        // </summary>
        internal virtual NewRecordOp CreateNewRecordOp(
            TypeUsage type,
            List<EdmProperty> fields)
        {
            return new NewRecordOp(type, fields);
        }

        // <summary>
        // Creates a new VarRefOp
        // </summary>
        // <param name="v"> The variable to reference </param>
        // <returns> A new VarRefOp that references the specified variable </returns>
        internal virtual VarRefOp CreateVarRefOp(Var v)
        {
            return new VarRefOp(v);
        }

        // <summary>
        // Creates a new ArithmeticOp of the specified type
        // </summary>
        // <param name="opType"> An OpType that specifies one of the valid arithmetic operations: Plus, Minus, Multiply, Divide, Modulo, UnaryMinus </param>
        // <param name="type"> Type metadata that specifies the result type of the arithmetic operation </param>
        // <returns> A new ArithmeticOp of the specified arithmetic OpType </returns>
        internal virtual ArithmeticOp CreateArithmeticOp(OpType opType, TypeUsage type)
        {
            return new ArithmeticOp(opType, type);
        }

        // <summary>
        // Creates a new PropertyOp
        // </summary>
        // <param name="prop"> EdmProperty metadata that specifies the property </param>
        // <returns> A new PropertyOp that references the specified property metadata </returns>
        internal PropertyOp CreatePropertyOp(EdmMember prop)
        {
            //
            // Track all rel-properties
            //
            var navProp = prop as NavigationProperty;
            if (navProp != null)
            {
                var relProperty = new RelProperty(navProp.RelationshipType, navProp.FromEndMember, navProp.ToEndMember);
                AddRelPropertyReference(relProperty);
                var inverseRelProperty = new RelProperty(navProp.RelationshipType, navProp.ToEndMember, navProp.FromEndMember);
                AddRelPropertyReference(inverseRelProperty);
            }

            // Actually create the propertyOp
            return new PropertyOp(Helper.GetModelTypeUsage(prop), prop);
        }

        // <summary>
        // Create a "relationship" propertyOp
        // </summary>
        // <param name="prop"> the relationship property </param>
        // <returns> a RelPropertyOp </returns>
        internal RelPropertyOp CreateRelPropertyOp(RelProperty prop)
        {
            AddRelPropertyReference(prop);
            return new RelPropertyOp(prop.ToEnd.TypeUsage, prop);
        }

        // <summary>
        // Creates a new RefOp
        // </summary>
        // <param name="entitySet"> The EntitySet to which the ref refers </param>
        // <param name="type"> The result type of the RefOp </param>
        // <returns> A new RefOp that references the specified EntitySet and has the specified result type </returns>
        internal virtual RefOp CreateRefOp(EntitySet entitySet, TypeUsage type)
        {
            return new RefOp(entitySet, type);
        }

        // <summary>
        // Creates a new ExistsOp
        // </summary>
        // <returns> A new ExistsOp </returns>
        internal ExistsOp CreateExistsOp()
        {
            return new ExistsOp(BooleanType);
        }

        // <summary>
        // Creates a new ElementOp
        // </summary>
        // <param name="type"> Type metadata that specifies the result (element) type </param>
        // <returns> A new ElementOp with the specified result type </returns>
        internal virtual ElementOp CreateElementOp(TypeUsage type)
        {
            return new ElementOp(type);
        }

        // <summary>
        // Creates a new GetEntityRefOp: a ref-extractor (from an entity instance) Op
        // </summary>
        // <param name="type"> Type metadata that specifies the result type </param>
        // <returns> A new GetEntityKeyOp with the specified result type </returns>
        internal virtual GetEntityRefOp CreateGetEntityRefOp(TypeUsage type)
        {
            return new GetEntityRefOp(type);
        }

        // <summary>
        // Creates a new GetRefKeyOp: a key-extractor (from a ref instance) Op
        // </summary>
        // <param name="type"> Type metadata that specifies the result type </param>
        // <returns> A new GetRefKeyOp with the specified result type </returns>
        internal virtual GetRefKeyOp CreateGetRefKeyOp(TypeUsage type)
        {
            return new GetRefKeyOp(type);
        }

        // <summary>
        // Creates a new CollectOp
        // </summary>
        // <param name="type"> Type metadata that specifies the result type of the Nest operation </param>
        // <returns> A new NestOp with the specified result type </returns>
        internal virtual CollectOp CreateCollectOp(TypeUsage type)
        {
            return new CollectOp(type);
        }

        // <summary>
        // Create a DerefOp
        // </summary>
        // <param name="type"> Entity type of the target entity </param>
        // <returns> a DerefOp </returns>
        internal virtual DerefOp CreateDerefOp(TypeUsage type)
        {
            return new DerefOp(type);
        }

        // <summary>
        // Create a new NavigateOp node
        // </summary>
        // <param name="type"> the output type of the navigateOp </param>
        // <param name="relProperty"> the relationship property </param>
        // <returns> the navigateOp </returns>
        internal NavigateOp CreateNavigateOp(TypeUsage type, RelProperty relProperty)
        {
            // keep track of rel-properties
            AddRelPropertyReference(relProperty);
            return new NavigateOp(type, relProperty);
        }

        #endregion

        #region AncillaryOps

        // <summary>
        // Creates a VarDefListOp
        // </summary>
        // <returns> A new VarDefListOp </returns>
        internal virtual VarDefListOp CreateVarDefListOp()
        {
            return VarDefListOp.Instance;
        }

        // <summary>
        // Creates a VarDefOp (for a computed var)
        // </summary>
        // <param name="v"> The computed var </param>
        // <returns> A new VarDefOp that references the computed var </returns>
        internal virtual VarDefOp CreateVarDefOp(Var v)
        {
            return new VarDefOp(v);
        }

        // <summary>
        // Create a VarDefOp and the associated node for an expression.
        // We create a computedVar first - of the same type as the expression, and
        // then create a VarDefOp for the computed Var. Finally, we create a Node for
        // the VarDefOp
        // </summary>
        // <param name="computedVar"> new Var produced </param>
        internal Node CreateVarDefNode(Node definingExpr, out Var computedVar)
        {
            DebugCheck.NotNull(definingExpr.Op);
            var scalarOp = definingExpr.Op as ScalarOp;
            Debug.Assert(scalarOp != null);
            computedVar = CreateComputedVar(scalarOp.Type);
            var varDefOp = CreateVarDefOp(computedVar);
            var varDefNode = CreateNode(varDefOp, definingExpr);
            return varDefNode;
        }

        // <summary>
        // Creates a VarDefListOp with a single child - a VarDefOp created as in the function
        // above.
        // </summary>
        // <param name="computedVar"> the computed Var produced </param>
        internal Node CreateVarDefListNode(Node definingExpr, out Var computedVar)
        {
            var varDefNode = CreateVarDefNode(definingExpr, out computedVar);
            var op = CreateVarDefListOp();
            var varDefListNode = CreateNode(op, varDefNode);
            return varDefListNode;
        }

        #endregion

        #region RelOps

        // <summary>
        // Creates a new ScanTableOp
        // </summary>
        // <param name="tableMetadata"> A Table metadata instance that specifies the table that should be scanned </param>
        // <returns> A new ScanTableOp that references a new Table instance based on the specified table metadata </returns>
        internal ScanTableOp CreateScanTableOp(TableMD tableMetadata)
        {
            var table = CreateTableInstance(tableMetadata);
            return CreateScanTableOp(table);
        }

        // <summary>
        // A variant of the above
        // </summary>
        // <param name="table"> The table instance </param>
        // <returns> a new ScanTableOp </returns>
        internal virtual ScanTableOp CreateScanTableOp(Table table)
        {
            return new ScanTableOp(table);
        }

        // <summary>
        // Creates an instance of a ScanViewOp
        // </summary>
        // <param name="table"> the table instance </param>
        // <returns> a new ScanViewOp </returns>
        internal virtual ScanViewOp CreateScanViewOp(Table table)
        {
            return new ScanViewOp(table);
        }

        // <summary>
        // Creates an instance of a ScanViewOp
        // </summary>
        // <param name="tableMetadata"> the table metadata </param>
        // <returns> a new ScanViewOp </returns>
        internal virtual ScanViewOp CreateScanViewOp(TableMD tableMetadata)
        {
            var table = CreateTableInstance(tableMetadata);
            return CreateScanViewOp(table);
        }

        // <summary>
        // Creates a new UnnestOp, which creates a streaming result from a scalar (non-RelOp) value
        // </summary>
        // <param name="v"> The Var that indicates the value to unnest </param>
        // <returns> A new UnnestOp that targets the specified Var </returns>
        internal virtual UnnestOp CreateUnnestOp(Var v)
        {
            var t = CreateTableInstance(Command.CreateTableDefinition(TypeHelpers.GetEdmType<CollectionType>(v.Type).TypeUsage));
            return CreateUnnestOp(v, t);
        }

        // <summary>
        // Creates a new UnnestOp - a variant of the above with the Table supplied
        // </summary>
        // <param name="v"> the unnest Var </param>
        // <param name="t"> the table instance </param>
        // <returns> a new UnnestOp </returns>
        internal virtual UnnestOp CreateUnnestOp(Var v, Table t)
        {
            return new UnnestOp(v, t);
        }

        // <summary>
        // Creates a new FilterOp
        // </summary>
        // <returns> A new FilterOp </returns>
        internal virtual FilterOp CreateFilterOp()
        {
            return FilterOp.Instance;
        }

        // <summary>
        // Creates a new ProjectOp
        // </summary>
        // <param name="vars"> A VarSet that specifies the Vars produced by the projection </param>
        // <returns> A new ProjectOp with the specified output VarSet </returns>
        internal virtual ProjectOp CreateProjectOp(VarVec vars)
        {
            return new ProjectOp(vars);
        }

        // <summary>
        // A variant of the above where the ProjectOp produces exactly one var
        // </summary>
        internal virtual ProjectOp CreateProjectOp(Var v)
        {
            var varSet = CreateVarVec();
            varSet.Set(v);
            return new ProjectOp(varSet);
        }

        #region JoinOps

        // <summary>
        // Creates a new InnerJoinOp
        // </summary>
        // <returns> A new InnerJoinOp </returns>
        internal virtual InnerJoinOp CreateInnerJoinOp()
        {
            return InnerJoinOp.Instance;
        }

        // <summary>
        // Creates a new LeftOuterJoinOp
        // </summary>
        // <returns> A new LeftOuterJoinOp </returns>
        internal virtual LeftOuterJoinOp CreateLeftOuterJoinOp()
        {
            return LeftOuterJoinOp.Instance;
        }

        // <summary>
        // Creates a new FullOuterJoinOp
        // </summary>
        // <returns> A new FullOuterJoinOp </returns>
        internal virtual FullOuterJoinOp CreateFullOuterJoinOp()
        {
            return FullOuterJoinOp.Instance;
        }

        // <summary>
        // Creates a new CrossJoinOp
        // </summary>
        // <returns> A new CrossJoinOp </returns>
        internal virtual CrossJoinOp CreateCrossJoinOp()
        {
            return CrossJoinOp.Instance;
        }

        #endregion

        #region ApplyOps

        // <summary>
        // Creates a new CrossApplyOp
        // </summary>
        // <returns> A new CrossApplyOp </returns>
        internal virtual CrossApplyOp CreateCrossApplyOp()
        {
            return CrossApplyOp.Instance;
        }

        // <summary>
        // Creates a new OuterApplyOp
        // </summary>
        // <returns> A new OuterApplyOp </returns>
        internal virtual OuterApplyOp CreateOuterApplyOp()
        {
            return OuterApplyOp.Instance;
        }

        #endregion

        #region SortKeys

        // <summary>
        // Creates a new SortKey with the specified var, order and collation
        // </summary>
        // <param name="v"> The variable to sort on </param>
        // <param name="asc"> The sort order (true for ascending, false for descending) </param>
        // <param name="collation"> The sort collation </param>
        // <returns> A new SortKey with the specified var, order and collation </returns>
        internal static SortKey CreateSortKey(Var v, bool asc, string collation)
        {
            return new SortKey(v, asc, collation);
        }

        // <summary>
        // Creates a new SortKey with the specified var and order
        // </summary>
        // <param name="v"> The variable to sort on </param>
        // <param name="asc"> The sort order (true for ascending, false for descending) </param>
        // <returns> A new SortKey with the specified var and order </returns>
        internal static SortKey CreateSortKey(Var v, bool asc)
        {
            return new SortKey(v, asc, "");
        }

        // <summary>
        // Creates a new SortKey with the specified var
        // </summary>
        // <param name="v"> The variable to sort on </param>
        // <returns> A new SortKey with the specified var </returns>
        internal static SortKey CreateSortKey(Var v)
        {
            return new SortKey(v, true, "");
        }

        #endregion

        // <summary>
        // Creates a new SortOp
        // </summary>
        // <param name="sortKeys"> The list of SortKeys that define the sort var, order and collation for each sort key </param>
        // <returns> A new SortOp with the specified sort keys </returns>
        internal virtual SortOp CreateSortOp(List<SortKey> sortKeys)
        {
            return new SortOp(sortKeys);
        }

        // <summary>
        // Creates a new ConstrainedSortOp
        // </summary>
        // <param name="sortKeys"> The list of SortKeys that define the sort var, order and collation for each sort key </param>
        // <returns> A new ConstrainedSortOp with the specified sort keys and a default WithTies value of false </returns>
        internal virtual ConstrainedSortOp CreateConstrainedSortOp(List<SortKey> sortKeys)
        {
            return new ConstrainedSortOp(sortKeys, false);
        }

        // <summary>
        // Creates a new ConstrainedSortOp
        // </summary>
        // <param name="sortKeys"> The list of SortKeys that define the sort var, order and collation for each sort key </param>
        // <param name="withTies"> The value to use for the WithTies property of the new ConstrainedSortOp </param>
        // <returns> A new ConstrainedSortOp with the specified sort keys and WithTies value </returns>
        internal virtual ConstrainedSortOp CreateConstrainedSortOp(List<SortKey> sortKeys, bool withTies)
        {
            return new ConstrainedSortOp(sortKeys, withTies);
        }

        // <summary>
        // Creates a new GroupByOp
        // </summary>
        // <param name="gbyKeys"> A VarSet that specifies the Key variables produced by the GroupByOp </param>
        // <param name="outputs"> A VarSet that specifies all (Key and Aggregate) variables produced by the GroupByOp </param>
        // <returns> A new GroupByOp with the specified key and output VarSets </returns>
        internal virtual GroupByOp CreateGroupByOp(VarVec gbyKeys, VarVec outputs)
        {
            return new GroupByOp(gbyKeys, outputs);
        }

        // <summary>
        // Creates a new GroupByIntoOp
        // </summary>
        // <param name="gbyKeys"> A VarSet that specifies the Key variables produced by the GroupByOp </param>
        // <param name="inputs"> A VarSet that specifies all (Key and Aggregate) variables produced by the GroupByOp </param>
        // <param name="outputs"> A VarSet that specifies the vars from the input that represent the real grouping input </param>
        // <returns> A new GroupByOp with the specified key and output VarSets </returns>
        internal virtual GroupByIntoOp CreateGroupByIntoOp(VarVec gbyKeys, VarVec inputs, VarVec outputs)
        {
            return new GroupByIntoOp(gbyKeys, inputs, outputs);
        }

        // <summary>
        // Creates a new DistinctOp
        // </summary>
        // <param name="keyVars"> list of key vars </param>
        // <returns> A new DistinctOp </returns>
        internal virtual DistinctOp CreateDistinctOp(VarVec keyVars)
        {
            return new DistinctOp(keyVars);
        }

        // <summary>
        // An overload of the above - where the distinct has exactly one key
        // </summary>
        internal virtual DistinctOp CreateDistinctOp(Var keyVar)
        {
            return new DistinctOp(CreateVarVec(keyVar));
        }

        // <summary>
        // Creates a new UnionAllOp
        // </summary>
        // <param name="leftMap"> Mappings from the Output Vars to the Vars produced by the left argument </param>
        // <param name="rightMap"> Mappings from the Output Vars to the Vars produced by the right argument </param>
        // <returns> A UnionAllOp that references the specified left and right Vars </returns>
        internal virtual UnionAllOp CreateUnionAllOp(VarMap leftMap, VarMap rightMap)
        {
            return CreateUnionAllOp(leftMap, rightMap, null);
        }

        // <summary>
        // Creates a new UnionAllOp, with a branch descriminator.
        // </summary>
        // <param name="leftMap"> Mappings from the Output Vars to the Vars produced by the left argument </param>
        // <param name="rightMap"> Mappings from the Output Vars to the Vars produced by the right argument </param>
        // <param name="branchDiscriminator"> Var that contains the branch discrimination value (may be null until key pullup occurs) </param>
        // <returns> A UnionAllOp that references the specified left and right Vars </returns>
        internal virtual UnionAllOp CreateUnionAllOp(VarMap leftMap, VarMap rightMap, Var branchDiscriminator)
        {
            Debug.Assert(leftMap.Count == rightMap.Count, "VarMap count mismatch");
            var vec = CreateVarVec();
            foreach (var v in leftMap.Keys)
            {
                vec.Set(v);
            }
            return new UnionAllOp(vec, leftMap, rightMap, branchDiscriminator);
        }

        // <summary>
        // Creates a new IntersectOp
        // </summary>
        // <param name="leftMap"> Mappings from the Output Vars to the Vars produced by the left argument </param>
        // <param name="rightMap"> Mappings from the Output Vars to the Vars produced by the right argument </param>
        // <returns> An IntersectOp that references the specified left and right Vars </returns>
        internal virtual IntersectOp CreateIntersectOp(VarMap leftMap, VarMap rightMap)
        {
            Debug.Assert(leftMap.Count == rightMap.Count, "VarMap count mismatch");
            var vec = CreateVarVec();
            foreach (var v in leftMap.Keys)
            {
                vec.Set(v);
            }
            return new IntersectOp(vec, leftMap, rightMap);
        }

        // <summary>
        // Creates a new ExceptOp
        // </summary>
        // <param name="leftMap"> Mappings from the Output Vars to the Vars produced by the left argument </param>
        // <param name="rightMap"> Mappings from the Output Vars to the Vars produced by the right argument </param>
        // <returns> An ExceptOp that references the specified left and right Vars </returns>
        internal virtual ExceptOp CreateExceptOp(VarMap leftMap, VarMap rightMap)
        {
            Debug.Assert(leftMap.Count == rightMap.Count, "VarMap count mismatch");
            var vec = CreateVarVec();
            foreach (var v in leftMap.Keys)
            {
                vec.Set(v);
            }
            return new ExceptOp(vec, leftMap, rightMap);
        }

        // <summary>
        // Create a single-row-op (the relop analog of Element)
        // </summary>
        internal virtual SingleRowOp CreateSingleRowOp()
        {
            return SingleRowOp.Instance;
        }

        // <summary>
        // Create a SingleRowTableOp - a table with exactly one row (and no columns)
        // </summary>
        internal virtual SingleRowTableOp CreateSingleRowTableOp()
        {
            return SingleRowTableOp.Instance;
        }

        #endregion

        #region PhysicalOps

        // <summary>
        // Create a PhysicalProjectOp - with a columnMap describing the output
        // </summary>
        // <param name="outputVars"> list of output vars </param>
        // <param name="columnMap"> columnmap describing the output element </param>
        internal virtual PhysicalProjectOp CreatePhysicalProjectOp(VarList outputVars, SimpleCollectionColumnMap columnMap)
        {
            return new PhysicalProjectOp(outputVars, columnMap);
        }

        // <summary>
        // Create a physicalProjectOp - with a single column output
        // </summary>
        // <param name="outputVar"> the output element </param>
        internal virtual PhysicalProjectOp CreatePhysicalProjectOp(Var outputVar)
        {
            var varList = CreateVarList();
            varList.Add(outputVar);
            var varRefColumnMap = new VarRefColumnMap(outputVar);

            var collectionColumnMap = new SimpleCollectionColumnMap(
                TypeUtils.CreateCollectionType(varRefColumnMap.Type), // type
                null, // name
                varRefColumnMap, // element map
                new SimpleColumnMap[0], // keys
                new SimpleColumnMap[0]); // foreign keys
            return CreatePhysicalProjectOp(varList, collectionColumnMap);
        }

        // <summary>
        // Another overload - with an additional discriminatorValue.
        // Should this be a subtype instead?
        // </summary>
        // <param name="collectionVar"> the collectionVar </param>
        // <param name="columnMap"> column map for the collection element </param>
        // <param name="flattenedElementVars"> elementVars with any nested collections pulled up </param>
        // <param name="keys"> keys specific to this collection </param>
        // <param name="sortKeys"> sort keys specific to this collecion </param>
        // <param name="discriminatorValue"> discriminator value for this collection (under the current nestOp) </param>
        // <returns> a new CollectionInfo instance </returns>
        internal static CollectionInfo CreateCollectionInfo(
            Var collectionVar, ColumnMap columnMap, VarList flattenedElementVars, VarVec keys, List<SortKey> sortKeys,
            object discriminatorValue)
        {
            return new CollectionInfo(collectionVar, columnMap, flattenedElementVars, keys, sortKeys, discriminatorValue);
        }

        // <summary>
        // Create a singleStreamNestOp
        // </summary>
        // <param name="keys"> keys for the nest operation </param>
        // <param name="prefixSortKeys"> list of prefix sort keys </param>
        // <param name="postfixSortKeys"> list of postfix sort keys </param>
        // <param name="outputVars"> List of outputVars </param>
        // <param name="collectionInfoList"> CollectionInfo for each collection </param>
        // <param name="discriminatorVar"> Var describing the discriminator </param>
        internal virtual SingleStreamNestOp CreateSingleStreamNestOp(
            VarVec keys,
            List<SortKey> prefixSortKeys, List<SortKey> postfixSortKeys,
            VarVec outputVars,
            List<CollectionInfo> collectionInfoList, Var discriminatorVar)
        {
            return new SingleStreamNestOp(keys, prefixSortKeys, postfixSortKeys, outputVars, collectionInfoList, discriminatorVar);
        }

        // <summary>
        // Create a MultiStreamNestOp
        // </summary>
        // <param name="prefixSortKeys"> list of prefix sort keys </param>
        // <param name="outputVars"> List of outputVars </param>
        // <param name="collectionInfoList"> CollectionInfo for each collection element </param>
        internal virtual MultiStreamNestOp CreateMultiStreamNestOp(
            List<SortKey> prefixSortKeys, VarVec outputVars,
            List<CollectionInfo> collectionInfoList)
        {
            return new MultiStreamNestOp(prefixSortKeys, outputVars, collectionInfoList);
        }

        #endregion

        #region NodeInfo

        // <summary>
        // Get auxilliary information for a Node
        // </summary>
        // <param name="n"> the node </param>
        // <returns> node info for this node </returns>
        internal virtual NodeInfo GetNodeInfo(Node n)
        {
            return n.GetNodeInfo(this);
        }

        // <summary>
        // Get extended node information for a RelOpNode
        // </summary>
        // <param name="n"> the node </param>
        // <returns> extended node info for this node </returns>
        internal virtual ExtendedNodeInfo GetExtendedNodeInfo(Node n)
        {
            return n.GetExtendedNodeInfo(this);
        }

        // <summary>
        // Recompute the nodeinfo for a node, but only if has already been computed
        // </summary>
        // <param name="n"> Node in question </param>
        internal virtual void RecomputeNodeInfo(Node n)
        {
            m_nodeInfoVisitor.RecomputeNodeInfo(n);
        }

        #endregion

        #region KeyInfo

        // <summary>
        // Pulls up keys if necessary and gets the key information for a Node
        // </summary>
        // <param name="n"> node </param>
        // <returns> key information </returns>
        internal virtual KeyVec PullupKeys(Node n)
        {
            return m_keyPullupVisitor.GetKeys(n);
        }

        #endregion

        #region Type Comparisons

        //
        // The functions described in this region are used through out the 
        // PlanCompiler to reason about type equality. Make sure that you 
        // use these and these alone
        //

        // <summary>
        // Check to see if two types are considered "equal" for the purposes
        // of the plan compiler.
        // Two types are considered to be equal if their "identities" are equal.
        // </summary>
        // <returns> true, if the types are "equal" </returns>
        internal static bool EqualTypes(TypeUsage x, TypeUsage y)
        {
            return TypeUsageEqualityComparer.Instance.Equals(x, y);
        }

        // <summary>
        // Check to see if two types are considered "equal" for the purposes
        // of the plan compiler
        // </summary>
        // <returns> true, if the types are "equal" </returns>
        internal static bool EqualTypes(EdmType x, EdmType y)
        {
            return TypeUsageEqualityComparer.Equals(x, y);
        }

        #endregion

        #region Builder Methods

        // <summary>
        // Builds out a UNION-ALL ladder from a sequence of node,var pairs.
        // Assumption: Each node produces exactly one Var
        // If the input sequence has zero elements, we return null
        // If the input sequence has one element, we return that single element
        // Otherwise, we build out a UnionAll ladder from each of the inputs. If the input sequence was {A,B,C,D},
        // we build up a union-all ladder that looks like
        // (((A UA B) UA C) UA D)
        // </summary>
        // <param name="inputNodes"> list of input nodes - one for each branch </param>
        // <param name="inputVars"> list of input vars - N for each branch </param>
        // <param name="resultNode"> the resulting union-all subtree </param>
        // <param name="resultVars"> the output vars from the union-all subtree </param>
        internal virtual void BuildUnionAllLadder(
            IList<Node> inputNodes, IList<Var> inputVars,
            out Node resultNode, out IList<Var> resultVars)
        {
            if (inputNodes.Count == 0)
            {
                resultNode = null;
                resultVars = null;
                return;
            }

            var varPerNode = inputVars.Count / inputNodes.Count;
            Debug.Assert(
                (inputVars.Count % inputNodes.Count == 0) && (varPerNode >= 1),
                "Inconsistent nodes/vars count:" + inputNodes.Count + "," + inputVars.Count);

            if (inputNodes.Count == 1)
            {
                resultNode = inputNodes[0];
                resultVars = inputVars;
                return;
            }

            var unionAllVars = new List<Var>();

            var unionAllNode = inputNodes[0];
            for (var j = 0; j < varPerNode; j++)
            {
                unionAllVars.Add(inputVars[j]);
            }

            for (var i = 1; i < inputNodes.Count; i++)
            {
                var leftVarMap = new VarMap();
                var rightVarMap = new VarMap();
                var setOpVars = new List<Var>();
                for (var j = 0; j < varPerNode; j++)
                {
                    var newVar = CreateSetOpVar(unionAllVars[j].Type);
                    setOpVars.Add(newVar);
                    leftVarMap.Add(newVar, unionAllVars[j]);
                    rightVarMap.Add(newVar, inputVars[i * varPerNode + j]);
                }
                Op unionAllOp = CreateUnionAllOp(leftVarMap, rightVarMap);
                unionAllNode = CreateNode(unionAllOp, unionAllNode, inputNodes[i]);
                unionAllVars = setOpVars;
            }

            resultNode = unionAllNode;
            resultVars = unionAllVars;
        }

        // <summary>
        // A simplified version of the method above - each branch can produce only one var
        // </summary>
        internal virtual void BuildUnionAllLadder(
            IList<Node> inputNodes, IList<Var> inputVars,
            out Node resultNode, out Var resultVar)
        {
            Debug.Assert(inputNodes.Count == inputVars.Count, "Count mismatch:" + inputNodes.Count + "," + inputVars.Count);
            IList<Var> varList;
            BuildUnionAllLadder(inputNodes, inputVars, out resultNode, out varList);
            if (varList != null
                && varList.Count > 0)
            {
                resultVar = varList[0];
            }
            else
            {
                resultVar = null;
            }
        }

        // <summary>
        // Build a projectOp tree over the input.
        // This function builds a projectOp tree over the input. The Outputs (vars) of the project are the
        // list of vars from the input (inputVars), plus one computed Var for each of the computed expressions
        // (computedExpressions)
        // </summary>
        // <param name="inputNode"> the input relop to the project </param>
        // <param name="inputVars"> List of vars from the input that need to be projected </param>
        // <param name="computedExpressions"> list (possibly empty) of any computed expressions </param>
        internal virtual Node BuildProject(
            Node inputNode, IEnumerable<Var> inputVars,
            IEnumerable<Node> computedExpressions)
        {
            Debug.Assert(inputNode.Op.IsRelOp, "Expected a RelOp. Found " + inputNode.Op.OpType);

            var varDefListOp = CreateVarDefListOp();
            var varDefListNode = CreateNode(varDefListOp);
            var projectVars = CreateVarVec(inputVars);
            foreach (var expr in computedExpressions)
            {
                Var v = CreateComputedVar(expr.Op.Type);
                projectVars.Set(v);
                var varDefOp = CreateVarDefOp(v);
                var varDefNode = CreateNode(varDefOp, expr);
                varDefListNode.Children.Add(varDefNode);
            }
            var projectNode = CreateNode(
                CreateProjectOp(projectVars),
                inputNode,
                varDefListNode);
            return projectNode;
        }

        // <summary>
        // A "simpler" builder method for ProjectOp. The assumption is that the only output is the
        // (var corresponding to) the computedExpression. None of the Vars of the "input" are projected out
        // The single output Var is returned in the "outputVar" parameter
        // </summary>
        // <param name="input"> the input relop </param>
        // <param name="computedExpression"> the computed expression </param>
        // <param name="projectVar"> (output) the computed var corresponding to the computed expression </param>
        // <returns> the new project subtree node </returns>
        internal virtual Node BuildProject(Node input, Node computedExpression, out Var projectVar)
        {
            var projectNode = BuildProject(input, new Var[] { }, new[] { computedExpression });
            projectVar = ((ProjectOp)projectNode.Op).Outputs.First;
            return projectNode;
        }

        // <summary>
        // Build the equivalent of an OfTypeExpression over the input (ie) produce the set of values from the
        // input that are of the desired type (exactly of the desired type, if the "includeSubtypes" parameter is false).
        // Further more, "update" the result element type to be the desired type.
        // We accomplish this by first building a FilterOp with an IsOf (or an IsOfOnly) predicate for the desired
        // type. We then build out a ProjectOp over the FilterOp, where we introduce a "Fake" TreatOp over the input
        // element to cast it to the right type. The "Fake" TreatOp is only there for "compile-time" typing reasons,
        // and will be ignored in the rest of the plan compiler
        // </summary>
        // <param name="inputNode"> the input collection </param>
        // <param name="inputVar"> the single Var produced by the input collection </param>
        // <param name="desiredType"> the desired element type </param>
        // <param name="includeSubtypes"> do we include subtypes of the desired element type </param>
        // <param name="resultNode"> the result subtree </param>
        // <param name="resultVar"> the single Var produced by the result subtree </param>
        internal virtual void BuildOfTypeTree(
            Node inputNode, Var inputVar, TypeUsage desiredType, bool includeSubtypes,
            out Node resultNode, out Var resultVar)
        {
            Op isOfOp = includeSubtypes ? CreateIsOfOp(desiredType) : CreateIsOfOnlyOp(desiredType);
            var predicate = CreateNode(isOfOp, CreateNode(CreateVarRefOp(inputVar)));
            var filterNode = CreateNode(CreateFilterOp(), inputNode, predicate);

            resultNode = BuildFakeTreatProject(filterNode, inputVar, desiredType, out resultVar);
        }

        // <summary>
        // Builds out a ProjectOp over the input that introduces a "Fake" TreatOp over the input Var to cast it to the desired type
        // The "Fake" TreatOp is only there for "compile-time" typing reasons, and will be ignored in the rest of the plan compiler.
        // </summary>
        // <param name="inputNode"> the input collection </param>
        // <param name="inputVar"> the single Var produced by the input collection </param>
        // <param name="desiredType"> the desired element type </param>
        // <param name="resultVar"> the single Var produced by the result subtree </param>
        // <returns> the result subtree </returns>
        internal virtual Node BuildFakeTreatProject(Node inputNode, Var inputVar, TypeUsage desiredType, out Var resultVar)
        {
            var treatNode = CreateNode(
                CreateFakeTreatOp(desiredType),
                CreateNode(CreateVarRefOp(inputVar)));
            var resultNode = BuildProject(inputNode, treatNode, out resultVar);
            return resultNode;
        }

        // <summary>
        // Build a comparisonOp over the input arguments. Build SoftCasts over the inputs, if we need
        // to.
        // </summary>
        // <param name="opType"> the comparison optype </param>
        // <param name="arg0"> Arg 0 </param>
        // <param name="arg1"> Arg 1 </param>
        // <param name="useDatabaseNullSemantics"> Specifies whether database null comparison behavior is enabled. </param>
        // <returns> the resulting comparison tree </returns>
        internal Node BuildComparison(OpType opType, Node arg0, Node arg1, bool useDatabaseNullSemantics = false)
        {
            if (!EqualTypes(arg0.Op.Type, arg1.Op.Type))
            {
                var commonType = TypeHelpers.GetCommonTypeUsage(arg0.Op.Type, arg1.Op.Type);
                Debug.Assert(commonType != null, "No common type for " + arg0.Op.Type + " and " + arg1.Op.Type);
                if (!EqualTypes(commonType, arg0.Op.Type))
                {
                    arg0 = CreateNode(CreateSoftCastOp(commonType), arg0);
                }
                if (!EqualTypes(commonType, arg1.Op.Type))
                {
                    arg1 = CreateNode(CreateSoftCastOp(commonType), arg1);
                }
            }

            var newNode = CreateNode(CreateComparisonOp(opType, useDatabaseNullSemantics), arg0, arg1);
            return newNode;
        }

        // <summary>
        // Build up a CollectOp over a relop tree
        // </summary>
        // <param name="relOpNode"> the relop tree </param>
        // <param name="relOpVar"> the single output var from the relop tree </param>
        internal virtual Node BuildCollect(Node relOpNode, Var relOpVar)
        {
            var physicalProjectNode = CreateNode(CreatePhysicalProjectOp(relOpVar), relOpNode);
            var collectOpType = TypeHelpers.CreateCollectionTypeUsage(relOpVar.Type);
            var collectNode = CreateNode(CreateCollectOp(collectOpType), physicalProjectNode);
            return collectNode;
        }

        #endregion

        #region Rel Properties

        // <summary>
        // Mark this rel-property as "referenced" in the current query, if the target
        // end has multiplicity of one (or zero_or_one)
        // </summary>
        // <param name="relProperty"> the rel-property </param>
        private void AddRelPropertyReference(RelProperty relProperty)
        {
            if (relProperty.ToEnd.RelationshipMultiplicity != RelationshipMultiplicity.Many
                &&
                !m_referencedRelProperties.Contains(relProperty))
            {
                m_referencedRelProperties.Add(relProperty);
            }
        }

        // <summary>
        // The set of referenced rel properties in the current query
        // </summary>
        internal virtual HashSet<RelProperty> ReferencedRelProperties
        {
            get { return m_referencedRelProperties; }
        }

        // <summary>
        // Is this rel-property referenced in the query so far
        // </summary>
        // <param name="relProperty"> the rel-property </param>
        // <returns> true, if the rel property was referenced in the query </returns>
        internal virtual bool IsRelPropertyReferenced(RelProperty relProperty)
        {
            var ret = m_referencedRelProperties.Contains(relProperty);
            return ret;
        }

        #endregion

        #endregion
    }
}
