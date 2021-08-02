// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder.Internal
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Common.CommandTrees.Internal;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Metadata.Edm.Provider;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;

    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal static class ArgumentValidation
    {
        // The Metadata ReadOnlyCollection class conflicts with System.Collections.ObjectModel.ReadOnlyCollection...
        internal static ReadOnlyCollection<TElement> NewReadOnlyCollection<TElement>(IList<TElement> list)
        {
            return new ReadOnlyCollection<TElement>(list);
        }

        internal static void RequirePolymorphicType(TypeUsage type)
        {
            DebugCheck.NotNull(type);

            if (!TypeSemantics.IsPolymorphicType(type))
            {
                throw new ArgumentException(Strings.Cqt_General_PolymorphicTypeRequired(type.ToString()), "type");
            }
        }

        internal static void RequireCompatibleType(DbExpression expression, TypeUsage requiredResultType, string argumentName)
        {
            RequireCompatibleType(expression, requiredResultType, argumentName, -1);
        }

        private static void RequireCompatibleType(
            DbExpression expression, TypeUsage requiredResultType, string argumentName, int argumentIndex)
        {
            DebugCheck.NotNull(expression);
            DebugCheck.NotNull(requiredResultType);

            if (!TypeSemantics.IsStructurallyEqualOrPromotableTo(expression.ResultType, requiredResultType))
            {
                // Don't call FormatIndex unless an exception is actually being thrown
                if (argumentIndex != -1)
                {
                    argumentName = StringUtil.FormatIndex(argumentName, argumentIndex);
                }

                throw new ArgumentException(
                    Strings.Cqt_ExpressionLink_TypeMismatch(
                        expression.ResultType.ToString(),
                        requiredResultType.ToString()
                        ), argumentName);
            }
        }

        internal static void RequireCompatibleType(DbExpression expression, PrimitiveTypeKind requiredResultType, string argumentName)
        {
            RequireCompatibleType(expression, requiredResultType, argumentName, -1);
        }

        private static void RequireCompatibleType(
            DbExpression expression, PrimitiveTypeKind requiredResultType, string argumentName, int index)
        {
            DebugCheck.NotNull(expression);

            PrimitiveTypeKind valueTypeKind;
            var valueIsPrimitive = TypeHelpers.TryGetPrimitiveTypeKind(expression.ResultType, out valueTypeKind);
            if (!valueIsPrimitive
                || valueTypeKind != requiredResultType)
            {
                if (index != -1)
                {
                    argumentName = StringUtil.FormatIndex(argumentName, index);
                }

                throw new ArgumentException(
                    Strings.Cqt_ExpressionLink_TypeMismatch(
                        (valueIsPrimitive
                             ? Enum.GetName(typeof(PrimitiveTypeKind), valueTypeKind)
                             : expression.ResultType.ToString()),
                        Enum.GetName(typeof(PrimitiveTypeKind), requiredResultType)
                        ), argumentName);
            }
        }

        private static void RequireCompatibleType(
            DbExpression from, RelationshipEndMember end, bool allowAllRelationshipsInSameTypeHierarchy)
        {
            DebugCheck.NotNull(from);
            DebugCheck.NotNull(end);

            var endType = end.TypeUsage;
            if (!TypeSemantics.IsReferenceType(endType))
            {
                // The only relation end that is currently allowed to have a non-Reference type is the Child end of
                // a composition, in which case the end type must be an entity type. 
                //
                //// Debug.Assert(end.Relation.IsComposition && !end.IsParent && (end.Type is EntityType), "Relation end can only have non-Reference type if it is a Composition child end");

                endType = TypeHelpers.CreateReferenceTypeUsage(TypeHelpers.GetEdmType<EntityType>(endType));
            }

            if (allowAllRelationshipsInSameTypeHierarchy)
            {
                if (TypeHelpers.GetCommonTypeUsage(endType, from.ResultType) == null)
                {
                    throw new ArgumentException(Strings.Cqt_RelNav_WrongSourceType(endType.ToString()), "from");
                }
            }
            else if (!TypeSemantics.IsStructurallyEqualOrPromotableTo(from.ResultType.EdmType, endType.EdmType))
            {
                throw new ArgumentException(Strings.Cqt_RelNav_WrongSourceType(endType.ToString()), "from");
            }
        }

        internal static void RequireCollectionArgument<TExpressionType>(DbExpression argument)
        {
            DebugCheck.NotNull(argument);

            if (!TypeSemantics.IsCollectionType(argument.ResultType))
            {
                throw new ArgumentException(Strings.Cqt_Unary_CollectionRequired(typeof(TExpressionType).Name), "argument");
            }
        }

        internal static TypeUsage RequireCollectionArguments<TExpressionType>(DbExpression left, DbExpression right)
        {
            DebugCheck.NotNull(left);
            DebugCheck.NotNull(right);

            if (!TypeSemantics.IsCollectionType(left.ResultType)
                || !TypeSemantics.IsCollectionType(right.ResultType))
            {
                throw new ArgumentException(Strings.Cqt_Binary_CollectionsRequired(typeof(TExpressionType).Name));
            }

            var commonType = TypeHelpers.GetCommonTypeUsage(left.ResultType, right.ResultType);
            if (null == commonType)
            {
                throw new ArgumentException(Strings.Cqt_Binary_CollectionsRequired(typeof(TExpressionType).Name));
            }

            return commonType;
        }

        internal static TypeUsage RequireComparableCollectionArguments<TExpressionType>(DbExpression left, DbExpression right)
        {
            var resultType = RequireCollectionArguments<TExpressionType>(left, right);

            if (!TypeHelpers.IsSetComparableOpType(TypeHelpers.GetElementTypeUsage(left.ResultType)))
            {
                throw new ArgumentException(
                    Strings.Cqt_InvalidTypeForSetOperation(
                        TypeHelpers.GetElementTypeUsage(left.ResultType).Identity, typeof(TExpressionType).Name), "left");
            }

            if (!TypeHelpers.IsSetComparableOpType(TypeHelpers.GetElementTypeUsage(right.ResultType)))
            {
                throw new ArgumentException(
                    Strings.Cqt_InvalidTypeForSetOperation(
                        TypeHelpers.GetElementTypeUsage(right.ResultType).Identity, typeof(TExpressionType).Name), "right");
            }

            return resultType;
        }

        private static EnumerableValidator<TElementIn, TElementOut, TResult> CreateValidator<TElementIn, TElementOut, TResult>(
            IEnumerable<TElementIn> argument, string argumentName, Func<TElementIn, int, TElementOut> convertElement,
            Func<List<TElementOut>, TResult> createResult)
        {
            var ret = new EnumerableValidator<TElementIn, TElementOut, TResult>(argument, argumentName);
            ret.ConvertElement = convertElement;
            ret.CreateResult = createResult;
            return ret;
        }

        internal static DbExpressionList CreateExpressionList(
            IEnumerable<DbExpression> arguments, string argumentName, Action<DbExpression, int> validationCallback)
        {
            return CreateExpressionList(arguments, argumentName, false, validationCallback);
        }

        private static DbExpressionList CreateExpressionList(
            IEnumerable<DbExpression> arguments, string argumentName, bool allowEmpty, Action<DbExpression, int> validationCallback)
        {
            var ev = CreateValidator(
                arguments, argumentName,
                (exp, idx) =>
                    {
                        if (validationCallback != null)
                        {
                            validationCallback(exp, idx);
                        }
                        return exp;
                    },
                expList => new DbExpressionList(expList)
                );

            ev.AllowEmpty = allowEmpty;

            return ev.Validate();
        }

        private static DbExpressionList CreateExpressionList(
            IEnumerable<DbExpression> arguments, string argumentName, int expectedElementCount, Action<DbExpression, int> validationCallback)
        {
            var ev = CreateValidator(
                arguments, argumentName,
                (exp, idx) =>
                    {
                        if (validationCallback != null)
                        {
                            validationCallback(exp, idx);
                        }
                        return exp;
                    },
                (expList) => new DbExpressionList(expList)
                );

            ev.ExpectedElementCount = expectedElementCount;
            ev.AllowEmpty = false;

            return ev.Validate();
        }

        #region Aggregates and Sort Keys

        private static FunctionParameter[] GetExpectedParameters(EdmFunction function)
        {
            DebugCheck.NotNull(function);

            return function.Parameters.Where(p => p.Mode == ParameterMode.In || p.Mode == ParameterMode.InOut).ToArray();
        }

        internal static DbExpressionList ValidateFunctionAggregate(EdmFunction function, IEnumerable<DbExpression> args)
        {
            // Verify that the aggregate function is from the metadata collection and data space of the command tree.
            CheckFunction(function);

            // Verify that the function is actually a valid aggregate function.
            // For now, only a single argument is allowed.
            if (!TypeSemantics.IsAggregateFunction(function)
                || null == function.ReturnParameter)
            {
                throw new ArgumentException(Strings.Cqt_Aggregate_InvalidFunction, "function");
            }

            var expectedParams = GetExpectedParameters(function);
            var funcArgs = CreateExpressionList(
                args, "argument", expectedParams.Length, (exp, idx) =>
                    {
                        var paramType = expectedParams[idx].TypeUsage;
                        TypeUsage elementType = null;
                        if (TypeHelpers.TryGetCollectionElementType(paramType, out elementType))
                        {
                            paramType = elementType;
                        }

                        RequireCompatibleType(exp, paramType, "argument");
                    }
                );

            return funcArgs;
        }

        internal static void ValidateSortClause(DbExpression key)
        {
            if (!TypeHelpers.IsValidSortOpKeyType(key.ResultType))
            {
                throw new ArgumentException(Strings.Cqt_Sort_OrderComparable, "key");
            }
        }

        internal static void ValidateSortClause(DbExpression key, string collation)
        {
            ValidateSortClause(key);

            Check.NotEmpty(collation, "collation");

            if (!TypeSemantics.IsPrimitiveType(key.ResultType, PrimitiveTypeKind.String))
            {
                throw new ArgumentException(Strings.Cqt_Sort_NonStringCollationInvalid, "collation");
            }
        }

        #endregion

        #region DbLambda

        internal static ReadOnlyCollection<DbVariableReferenceExpression> ValidateLambda(
            IEnumerable<DbVariableReferenceExpression> variables)
        {
            var varVal = CreateValidator(
                variables, "variables",
                (varExp, idx) =>
                    {
                        if (null == varExp)
                        {
                            throw new ArgumentNullException(StringUtil.FormatIndex("variables", idx));
                        }
                        return varExp;
                    },
                (varList) => new ReadOnlyCollection<DbVariableReferenceExpression>(varList)
                );
            varVal.AllowEmpty = true;
            varVal.GetName = (varDef, idx) => varDef.VariableName;

            var result = varVal.Validate();
            return result;
        }

        #endregion

        #region Binding-based methods: All, Any, Cross|OuterApply, Cross|FullOuter|Inner|LeftOuterJoin, Filter, GroupBy, Project, Skip, Sort

        internal static TypeUsage ValidateQuantifier(DbExpression predicate)
        {
            RequireCompatibleType(predicate, PrimitiveTypeKind.Boolean, "predicate");

            return predicate.ResultType;
        }

        internal static TypeUsage ValidateApply(DbExpressionBinding input, DbExpressionBinding apply)
        {
            // Duplicate Input and Apply binding names are not allowed
            if (input.VariableName.Equals(apply.VariableName, StringComparison.Ordinal))
            {
                throw new ArgumentException(Strings.Cqt_Apply_DuplicateVariableNames);
            }

            // Initialize the result type
            var recordCols = new List<KeyValuePair<string, TypeUsage>>();
            recordCols.Add(new KeyValuePair<string, TypeUsage>(input.VariableName, input.VariableType));
            recordCols.Add(new KeyValuePair<string, TypeUsage>(apply.VariableName, apply.VariableType));

            return CreateCollectionOfRowResultType(recordCols);
        }

        internal static ReadOnlyCollection<DbExpressionBinding> ValidateCrossJoin(
            IEnumerable<DbExpressionBinding> inputs, out TypeUsage resultType)
        {
            // Validate the input expression bindings and build the column types for the record type
            // that will be the element type of the collection of record type result type of the join.
            var inputList = new List<DbExpressionBinding>();
            var columns = new List<KeyValuePair<string, TypeUsage>>();
            var bindingNames = new Dictionary<string, int>();
            var inputEnum = inputs.GetEnumerator();
            var iPos = 0;
            while (inputEnum.MoveNext())
            {
                var input = inputEnum.Current;

                // Validate the DbExpressionBinding before accessing its properties
                var argumentName = StringUtil.FormatIndex("inputs", iPos);
                if (input == null)
                {
                    throw new ArgumentNullException(argumentName);
                }

                // Duplicate binding names are not allowed
                var nameIndex = -1;
                if (bindingNames.TryGetValue(input.VariableName, out nameIndex))
                {
                    throw new ArgumentException(Strings.Cqt_CrossJoin_DuplicateVariableNames(nameIndex, iPos, input.VariableName));
                }

                inputList.Add(input);
                bindingNames.Add(input.VariableName, iPos);

                columns.Add(new KeyValuePair<string, TypeUsage>(input.VariableName, input.VariableType));

                iPos++;
            }

            if (inputList.Count < 2)
            {
                throw new ArgumentException(Strings.Cqt_CrossJoin_AtLeastTwoInputs, "inputs");
            }

            // Initialize the result type
            resultType = CreateCollectionOfRowResultType(columns);

            // Initialize state
            return new ReadOnlyCollection<DbExpressionBinding>(inputList);
        }

        internal static TypeUsage ValidateJoin(DbExpressionBinding left, DbExpressionBinding right, DbExpression joinCondition)
        {
            // Duplicate Left and Right binding names are not allowed
            if (left.VariableName.Equals(right.VariableName, StringComparison.Ordinal))
            {
                throw new ArgumentException(Strings.Cqt_Join_DuplicateVariableNames);
            }

            // Validate the JoinCondition)
            RequireCompatibleType(joinCondition, PrimitiveTypeKind.Boolean, "joinCondition");

            // Initialize the result type
            var columns = new List<KeyValuePair<string, TypeUsage>>(2);
            columns.Add(new KeyValuePair<string, TypeUsage>(left.VariableName, left.VariableType));
            columns.Add(new KeyValuePair<string, TypeUsage>(right.VariableName, right.VariableType));

            return CreateCollectionOfRowResultType(columns);
        }

        internal static TypeUsage ValidateFilter(DbExpressionBinding input, DbExpression predicate)
        {
            RequireCompatibleType(predicate, PrimitiveTypeKind.Boolean, "predicate");
            return input.Expression.ResultType;
        }

        internal static TypeUsage ValidateGroupBy(
            IEnumerable<KeyValuePair<string, DbExpression>> keys,
            IEnumerable<KeyValuePair<string, DbAggregate>> aggregates, out DbExpressionList validKeys,
            out ReadOnlyCollection<DbAggregate> validAggregates)
        {
            // Track the cumulative set of column names and types, as well as key column names
            var columns = new List<KeyValuePair<string, TypeUsage>>();
            var keyNames = new HashSet<string>();

            // Validate the grouping keys
            var keyValidator = CreateValidator(
                keys, "keys",
                (keyInfo, index) =>
                    {
                        CheckNamed(keyInfo, "keys", index);

                        // The result Type of an expression used as a group key must be equality comparable
                        if (!TypeHelpers.IsValidGroupKeyType(keyInfo.Value.ResultType))
                        {
                            throw new ArgumentException(Strings.Cqt_GroupBy_KeyNotEqualityComparable(keyInfo.Key));
                        }

                        keyNames.Add(keyInfo.Key);
                        columns.Add(new KeyValuePair<string, TypeUsage>(keyInfo.Key, keyInfo.Value.ResultType));

                        return keyInfo.Value;
                    },
                expList => new DbExpressionList(expList)
                );
            keyValidator.AllowEmpty = true;
            keyValidator.GetName = (keyInfo, idx) => keyInfo.Key;
            validKeys = keyValidator.Validate();

            var hasGroupAggregate = false;
            var aggValidator = CreateValidator(
                aggregates, "aggregates",
                (aggInfo, idx) =>
                    {
                        CheckNamed(aggInfo, "aggregates", idx);

                        // Is there a grouping key with the same name?
                        if (keyNames.Contains(aggInfo.Key))
                        {
                            throw new ArgumentException(Strings.Cqt_GroupBy_AggregateColumnExistsAsGroupColumn(aggInfo.Key));
                        }

                        // At most one group aggregate can be specified
                        if (aggInfo.Value is DbGroupAggregate)
                        {
                            if (hasGroupAggregate)
                            {
                                throw new ArgumentException(Strings.Cqt_GroupBy_MoreThanOneGroupAggregate);
                            }
                            else
                            {
                                hasGroupAggregate = true;
                            }
                        }

                        columns.Add(new KeyValuePair<string, TypeUsage>(aggInfo.Key, aggInfo.Value.ResultType));
                        return aggInfo.Value;
                    },
                aggList => NewReadOnlyCollection(aggList)
                );
            aggValidator.AllowEmpty = true;
            aggValidator.GetName = (aggInfo, idx) => aggInfo.Key;
            validAggregates = aggValidator.Validate();

            // Either the Keys or Aggregates may be omitted, but not both
            if (0 == validKeys.Count
                && 0 == validAggregates.Count)
            {
                throw new ArgumentException(Strings.Cqt_GroupBy_AtLeastOneKeyOrAggregate);
            }

            // Create the result type. This is a collection of the record type produced by the group keys and aggregates.
            return CreateCollectionOfRowResultType(columns);
        }

        // <summary>
        // Validates the input and sort key arguments to both DbSkipExpression and DbSortExpression.
        // </summary>
        // <param name="sortOrder"> A list of SortClauses that specifies the sort order to apply to the input collection </param>
        internal static ReadOnlyCollection<DbSortClause> ValidateSortArguments(IEnumerable<DbSortClause> sortOrder)
        {
            var ev = CreateValidator(
                sortOrder, "sortOrder",
                (key, idx) => key,
                keyList => NewReadOnlyCollection(keyList)
                );
            ev.AllowEmpty = false;
            return ev.Validate();
        }

        internal static ReadOnlyCollection<DbSortClause> ValidateSort(IEnumerable<DbSortClause> sortOrder)
        {
            // Validate the input expression binding and sort keys
            return ValidateSortArguments(sortOrder);
        }

        #endregion

        #region Leaf Expressions - Null, Constant, Parameter, Scan

        internal static TypeUsage ValidateConstant(Type type)
        {
            // Check that type is actually a valid constant (i.e. primitive) type
            PrimitiveTypeKind primitiveTypeKind;
            if (!TryGetPrimitiveTypeKind(type, out primitiveTypeKind))
            {
                throw new ArgumentException(Strings.Cqt_Constant_InvalidType, "type");
            }

            return TypeHelpers.GetLiteralTypeUsage(primitiveTypeKind);
        }

        internal static TypeUsage ValidateConstant(object value)
        {
            // Check that typeof(value) is actually a valid constant (i.e. primitive) type
            return ValidateConstant(value.GetType());
        }

        internal static void ValidateConstant(TypeUsage constantType, object value)
        {
            CheckType(constantType, "constantType");

            // Verify that constantType is a primitive or enum type and that the value is an instance of that type
            // Note that the value is not validated against applicable facets (such as MaxLength for a string value),
            // this is left to the server.
            EnumType edmEnumType;
            if (TypeHelpers.TryGetEdmType(constantType, out edmEnumType))
            {
                // type of the value has to match the edm enum type or underlying types have to be the same
                var clrEnumUnderlyingType = edmEnumType.UnderlyingType.ClrEquivalentType;
                if (clrEnumUnderlyingType != value.GetType()
                    && !(value.GetType().IsEnum() && ClrEdmEnumTypesMatch(edmEnumType, value.GetType())))
                {
                    throw new ArgumentException(
                        Strings.Cqt_Constant_ClrEnumTypeDoesNotMatchEdmEnumType(
                            value.GetType().Name,
                            edmEnumType.Name,
                            clrEnumUnderlyingType.Name), "value");
                }
            }
            else
            {
                PrimitiveType primitiveType;
                if (!TypeHelpers.TryGetEdmType(constantType, out primitiveType))
                {
                    throw new ArgumentException(Strings.Cqt_Constant_InvalidConstantType(constantType.ToString()), "constantType");
                }

                PrimitiveTypeKind valueKind;
                if (!TryGetPrimitiveTypeKind(value.GetType(), out valueKind)
                    ||
                    primitiveType.PrimitiveTypeKind != valueKind)
                {
                    // there are only two O-space types for the 16 C-space spatial types.   Allow constants of any geography type to be represented as DbGeography, and
                    // any geometric type to be represented by Dbgeometry.
                    if (!(Helper.IsGeographicType(primitiveType) && valueKind == PrimitiveTypeKind.Geography)
                        && !(Helper.IsGeometricType(primitiveType) && valueKind == PrimitiveTypeKind.Geometry))
                    {
                        throw new ArgumentException(Strings.Cqt_Constant_InvalidValueForType(constantType.ToString()), "value");
                    }
                }
            }
        }

        #endregion

        #region Ref Operators - Deref, EntityRef, Ref, RefKey, RelationshipNavigation

        internal static TypeUsage ValidateCreateRef(
            EntitySet entitySet, EntityType entityType, IEnumerable<DbExpression> keyValues, out DbExpression keyConstructor)
        {
            CheckEntitySet(entitySet, "entitySet");
            CheckType(entityType, "entityType");

            // Verify that the specified return type of the Ref operation is actually in
            // the same hierarchy as the Entity type of the specified Entity set.
            if (!TypeSemantics.IsValidPolymorphicCast(entitySet.ElementType, entityType))
            {
                throw new ArgumentException(Strings.Cqt_Ref_PolymorphicArgRequired);
            }

            // Validate the key values. The count of values must match the count of key members,
            // and each key value must have a result type that is compatible with the type of
            // the corresponding key member.
            IList<EdmMember> keyMembers = entityType.KeyMembers;
            var keyValueValidator = CreateValidator(
                keyValues, "keyValues",
                (valueExp, idx) =>
                    {
                        RequireCompatibleType(valueExp, keyMembers[idx].TypeUsage, "keyValues", idx);
                        return new KeyValuePair<string, DbExpression>(keyMembers[idx].Name, valueExp);
                    },
                (columnList) => columnList
                );
            keyValueValidator.ExpectedElementCount = keyMembers.Count;
            var keyColumns = keyValueValidator.Validate();

            keyConstructor = DbExpressionBuilder.NewRow(keyColumns);
            return CreateReferenceResultType(entityType);
        }

        internal static TypeUsage ValidateRefFromKey(EntitySet entitySet, DbExpression keyValues, EntityType entityType)
        {
            CheckEntitySet(entitySet, "entitySet");
            CheckType(entityType);

            // Verify that the specified return type of the Ref operation is actually in
            // the same hierarchy as the Entity type of the specified Entity set.
            if (!TypeSemantics.IsValidPolymorphicCast(entitySet.ElementType, entityType))
            {
                throw new ArgumentException(Strings.Cqt_Ref_PolymorphicArgRequired);
            }

            // The Argument DbExpression must construct a set of values of the same types as the Key members of the Entity
            // The names of the columns in the record type constructed by the Argument are not important, only that the
            // number of columns is the same as the number of Key members and that for each Key member the corresponding
            // column (based on order) is of a promotable type. 
            // To enforce this, the argument's result type is compared to a record type based on the names and types of
            // the Key members. Since the promotability check used in RequireCompatibleType will ignore the names of the
            // expected type's columns, RequireCompatibleType will therefore enforce the required level of type correctness
            //
            // Set the expected type to be the record type created based on the Key members
            var keyType = CreateResultType(TypeHelpers.CreateKeyRowType(entitySet.ElementType));
            RequireCompatibleType(keyValues, keyType, "keyValues");

            return CreateReferenceResultType(entityType);
        }

        internal static TypeUsage ValidateNavigate(
            DbExpression navigateFrom, RelationshipType type, string fromEndName, string toEndName, out RelationshipEndMember fromEnd,
            out RelationshipEndMember toEnd)
        {
            // Ensure that the relation type is non-null and from the same metadata workspace as the command tree
            CheckType(type);

            // Retrieve the relation end properties with the specified 'from' and 'to' names
            if (!type.RelationshipEndMembers.TryGetValue(fromEndName, false /*ignoreCase*/, out fromEnd))
            {
                throw new ArgumentOutOfRangeException(fromEndName, Strings.Cqt_Factory_NoSuchRelationEnd);
            }

            if (!type.RelationshipEndMembers.TryGetValue(toEndName, false /*ignoreCase*/, out toEnd))
            {
                throw new ArgumentOutOfRangeException(toEndName, Strings.Cqt_Factory_NoSuchRelationEnd);
            }

            // Validate the retrieved relation end against the navigation source
            RequireCompatibleType(navigateFrom, fromEnd, allowAllRelationshipsInSameTypeHierarchy: false);

            return CreateResultType(toEnd);
        }

        internal static TypeUsage ValidateNavigate(
            DbExpression navigateFrom, RelationshipEndMember fromEnd, RelationshipEndMember toEnd, out RelationshipType relType,
            bool allowAllRelationshipsInSameTypeHierarchy)
        {
            DebugCheck.NotNull(navigateFrom);

            // Validate the relationship ends before use
            CheckMember(fromEnd, "fromEnd");
            CheckMember(toEnd, "toEnd");

            relType = fromEnd.DeclaringType as RelationshipType;

            // Ensure that the relation type is non-null and read-only
            CheckType(relType);

            // Validate that the 'to' relationship end is defined by the same relationship type as the 'from' end
            if (!relType.Equals(toEnd.DeclaringType))
            {
                throw new ArgumentException(Strings.Cqt_Factory_IncompatibleRelationEnds, "toEnd");
            }

            RequireCompatibleType(navigateFrom, fromEnd, allowAllRelationshipsInSameTypeHierarchy);

            return CreateResultType(toEnd);
        }

        #endregion

        #region Unary and Binary Set Operators - Distinct, Element, IsEmpty, Except, Intersect, UnionAll, Limit

        internal static TypeUsage ValidateElement(DbExpression argument)
        {
            DebugCheck.NotNull(argument);

            // Ensure that the operand is actually of a collection type.
            RequireCollectionArgument<DbElementExpression>(argument);

            // Result Type is the element type of the collection type
            return TypeHelpers.GetEdmType<CollectionType>(argument.ResultType).TypeUsage;
        }

        #endregion

        #region General Operators - Case, Function, NewInstance, Property

        internal static TypeUsage ValidateCase(
            IEnumerable<DbExpression> whenExpressions, IEnumerable<DbExpression> thenExpressions, DbExpression elseExpression,
            out DbExpressionList validWhens, out DbExpressionList validThens)
        {
            // All 'When's must produce a Boolean result, and a common (non-null) result type must exist
            // for all 'Thens' and 'Else'. At least one When/Then clause is required and the number of
            // 'When's must equal the number of 'Then's.
            validWhens = CreateExpressionList(
                whenExpressions, "whenExpressions",
                (exp, idx) => { RequireCompatibleType(exp, PrimitiveTypeKind.Boolean, "whenExpressions", idx); }
                );
            Debug.Assert(validWhens.Count > 0, "CreateExpressionList(arguments, argumentName, validationCallback) allowed empty Whens?");

            TypeUsage commonResultType = null;
            validThens = CreateExpressionList(
                thenExpressions, "thenExpressions", (exp, idx) =>
                    {
                        if (null == commonResultType)
                        {
                            commonResultType = exp.ResultType;
                        }
                        else
                        {
                            commonResultType = TypeHelpers.GetCommonTypeUsage(
                                exp.ResultType, commonResultType);
                            if (null == commonResultType)
                            {
                                throw new ArgumentException(Strings.Cqt_Case_InvalidResultType);
                            }
                        }
                    }
                );
            Debug.Assert(validWhens.Count > 0, "CreateExpressionList(arguments, argumentName, validationCallback) allowed empty Thens?");

            commonResultType = TypeHelpers.GetCommonTypeUsage(elseExpression.ResultType, commonResultType);
            if (null == commonResultType)
            {
                throw new ArgumentException(Strings.Cqt_Case_InvalidResultType);
            }

            // The number of 'When's must equal the number of 'Then's.
            if (validWhens.Count != validThens.Count)
            {
                throw new ArgumentException(Strings.Cqt_Case_WhensMustEqualThens);
            }

            // The result type of DbCaseExpression is the common result type
            return commonResultType;
        }

        internal static TypeUsage ValidateFunction(
            EdmFunction function, IEnumerable<DbExpression> arguments, out DbExpressionList validArgs)
        {
            // Ensure that the function metadata is non-null and from the same metadata workspace and dataspace as the command tree.
            CheckFunction(function);

            // Non-composable functions or non-UDF functions including command text are not permitted in expressions -- they can only be 
            // executed independently
            if (!function.IsComposableAttribute)
            {
                throw new ArgumentException(Strings.Cqt_Function_NonComposableInExpression, "function");
            }
            if (!String.IsNullOrEmpty(function.CommandTextAttribute)
                && !function.HasUserDefinedBody)
            {
                throw new ArgumentException(Strings.Cqt_Function_CommandTextInExpression, "function");
            }

            // Functions that return void are not allowed
            if (null == function.ReturnParameter)
            {
                throw new ArgumentException(Strings.Cqt_Function_VoidResultInvalid, "function");
            }

            //
            // Validate the arguments
            //
            var expectedParams = GetExpectedParameters(function);
            validArgs = CreateExpressionList(
                arguments, "arguments", expectedParams.Length,
                (exp, idx) => { RequireCompatibleType(exp, expectedParams[idx].TypeUsage, "arguments", idx); }
                );

            return function.ReturnParameter.TypeUsage;
        }

        internal static TypeUsage ValidateInvoke(DbLambda lambda, IEnumerable<DbExpression> arguments, out DbExpressionList validArguments)
        {
            // Each argument must be type-compatible with the corresponding lambda variable for which it supplies the value
            validArguments = null;
            var argValidator = CreateValidator(
                arguments, "arguments", (exp, idx) =>
                    {
                        RequireCompatibleType(exp, lambda.Variables[idx].ResultType, "arguments", idx);
                        return exp;
                    },
                expList => new DbExpressionList(expList)
                );
            argValidator.ExpectedElementCount = lambda.Variables.Count;
            validArguments = argValidator.Validate();

            // The result type of the lambda expression is the result type of the lambda body
            return lambda.Body.ResultType;
        }

        internal static TypeUsage ValidateNewEmptyCollection(TypeUsage collectionType, out DbExpressionList validElements)
        {
            CheckType(collectionType, "collectionType");
            if (!TypeSemantics.IsCollectionType(collectionType))
            {
                throw new ArgumentException(Strings.Cqt_NewInstance_CollectionTypeRequired, "collectionType");
            }

            validElements = new DbExpressionList(new DbExpression[] { });
            return collectionType;
        }

        internal static TypeUsage ValidateNewRow(
            IEnumerable<KeyValuePair<string, DbExpression>> columnValues, out DbExpressionList validElements)
        {
            var columnTypes = new List<KeyValuePair<string, TypeUsage>>();
            var columnValidator = CreateValidator(
                columnValues, "columnValues", (columnValue, idx) =>
                    {
                        CheckNamed(columnValue, "columnValues", idx);
                        columnTypes.Add(
                            new KeyValuePair<string, TypeUsage>(columnValue.Key, columnValue.Value.ResultType));
                        return columnValue.Value;
                    },
                expList => new DbExpressionList(expList)
                );
            columnValidator.GetName = ((columnValue, idx) => columnValue.Key);
            validElements = columnValidator.Validate();
            return CreateResultType(TypeHelpers.CreateRowType(columnTypes));
        }

        internal static TypeUsage ValidateNew(
            TypeUsage instanceType, IEnumerable<DbExpression> arguments, out DbExpressionList validArguments)
        {
            // Ensure that the type is non-null, valid and not NullType
            CheckType(instanceType, "instanceType");

            CollectionType collectionType = null;
            if (TypeHelpers.TryGetEdmType(instanceType, out collectionType)
                && collectionType != null)
            {
                // Collection arguments may have zero count for empty collection construction
                var elementType = collectionType.TypeUsage;
                validArguments = CreateExpressionList(
                    arguments, "arguments", true, (exp, idx) => { RequireCompatibleType(exp, elementType, "arguments", idx); });
            }
            else
            {
                var expectedTypes = GetStructuralMemberTypes(instanceType);
                var pos = 0;
                validArguments = CreateExpressionList(
                    arguments, "arguments", expectedTypes.Count,
                    (exp, idx) => { RequireCompatibleType(exp, expectedTypes[pos++], "arguments", idx); });
            }

            return instanceType;
        }

        private static List<TypeUsage> GetStructuralMemberTypes(TypeUsage instanceType)
        {
            var structType = instanceType.EdmType as StructuralType;
            if (null == structType)
            {
                throw new ArgumentException(Strings.Cqt_NewInstance_StructuralTypeRequired, "instanceType");
            }

            if (structType.Abstract)
            {
                throw new ArgumentException(Strings.Cqt_NewInstance_CannotInstantiateAbstractType(instanceType.ToString()), "instanceType");
            }

            var members = TypeHelpers.GetAllStructuralMembers(structType);
            if (members == null
                || members.Count < 1)
            {
                throw new ArgumentException(
                    Strings.Cqt_NewInstance_CannotInstantiateMemberlessType(instanceType.ToString()), "instanceType");
            }

            var memberTypes = new List<TypeUsage>(members.Count);
            for (var idx = 0; idx < members.Count; idx++)
            {
                memberTypes.Add(Helper.GetModelTypeUsage(members[idx]));
            }
            return memberTypes;
        }

        internal static TypeUsage ValidateNewEntityWithRelationships(
            EntityType entityType, IEnumerable<DbExpression> attributeValues, IList<DbRelatedEntityRef> relationships,
            out DbExpressionList validArguments, out ReadOnlyCollection<DbRelatedEntityRef> validRelatedRefs)
        {
            DebugCheck.NotNull(entityType);
            DebugCheck.NotNull(attributeValues);
            DebugCheck.NotNull(relationships);

            var resultType = CreateResultType(entityType);
            resultType = ValidateNew(resultType, attributeValues, out validArguments);

            if (relationships.Count > 0)
            {
                var relatedRefs = new List<DbRelatedEntityRef>(relationships.Count);
                for (var idx = 0; idx < relationships.Count; idx++)
                {
                    var relatedRef = relationships[idx];
                    Debug.Assert(relatedRef != null);

                    // The source end type must be the same type or a supertype of the Entity instance type
                    var expectedSourceType = TypeHelpers.GetEdmType<RefType>(relatedRef.SourceEnd.TypeUsage).ElementType;
                    if (!entityType.EdmEquals(expectedSourceType)
                        &&
                        !entityType.IsSubtypeOf(expectedSourceType))
                    {
                        throw new ArgumentException(
                            Strings.Cqt_NewInstance_IncompatibleRelatedEntity_SourceTypeNotValid,
                            StringUtil.FormatIndex("relationships", idx));
                    }

                    relatedRefs.Add(relatedRef);
                }
                validRelatedRefs = new ReadOnlyCollection<DbRelatedEntityRef>(relatedRefs);
            }
            else
            {
                validRelatedRefs = new ReadOnlyCollection<DbRelatedEntityRef>(new DbRelatedEntityRef[] { });
            }

            return resultType;
        }

        internal static TypeUsage ValidateProperty(DbExpression instance, string propertyName, bool ignoreCase, out EdmMember foundMember)
        {
            // EdmProperty, NavigationProperty and RelationshipEndMember are the only valid members for DbPropertyExpression.
            // Since these all derive from EdmMember they are declared by subtypes of StructuralType,
            // so a non-StructuralType instance is invalid.
            StructuralType structType;
            if (TypeHelpers.TryGetEdmType(instance.ResultType, out structType))
            {
                // Does the type declare a member with the given name?
                if (structType.Members.TryGetValue(propertyName, ignoreCase, out foundMember)
                    && foundMember != null)
                {
                    // If the member is a RelationshipEndMember, call the corresponding overload.
                    if (Helper.IsRelationshipEndMember(foundMember)
                        || Helper.IsEdmProperty(foundMember)
                        || Helper.IsNavigationProperty(foundMember))
                    {
                        return Helper.GetModelTypeUsage(foundMember);
                    }
                }
            }

            throw new ArgumentOutOfRangeException(
                "propertyName", Strings.NoSuchProperty(propertyName, instance.ResultType.ToString()));
        }

        #endregion

        private static void CheckNamed<T>(KeyValuePair<string, T> element, string argumentName, int index)
        {
            if (string.IsNullOrEmpty(element.Key))
            {
                if (index != -1)
                {
                    argumentName = StringUtil.FormatIndex(argumentName, index);
                }
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, "{0}.Key", argumentName));
            }

            if (null == element.Value)
            {
                if (index != -1)
                {
                    argumentName = StringUtil.FormatIndex(argumentName, index);
                }
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, "{0}.Value", argumentName));
            }
        }

        private static void CheckReadOnly(GlobalItem item, string varName)
        {
            DebugCheck.NotNull(item);
            if (!(item.IsReadOnly))
            {
                throw new ArgumentException(Strings.Cqt_General_MetadataNotReadOnly, varName);
            }
        }

        private static void CheckReadOnly(TypeUsage item, string varName)
        {
            DebugCheck.NotNull(item);
            if (!(item.IsReadOnly))
            {
                throw new ArgumentException(Strings.Cqt_General_MetadataNotReadOnly, varName);
            }
        }

        private static void CheckReadOnly(EntitySetBase item, string varName)
        {
            DebugCheck.NotNull(item);
            if (!(item.IsReadOnly))
            {
                throw new ArgumentException(Strings.Cqt_General_MetadataNotReadOnly, varName);
            }
        }

        private static void CheckType(EdmType type)
        {
            CheckType(type, "type");
        }

        private static void CheckType(EdmType type, string argumentName)
        {
            DebugCheck.NotNull(type);
            CheckReadOnly(type, argumentName);
        }

        // <summary>
        // Ensures that the  specified type is non-null, associated with the correct metadata workspace/dataspace, and is not NullType.
        // </summary>
        // <param name="type"> The type usage instance to verify. </param>
        // <exception cref="ArgumentNullException">If the specified type metadata is null</exception>
        // <exception cref="ArgumentException">If the specified type metadata belongs to a metadata workspace other than the workspace of the command tree</exception>
        // <exception cref="ArgumentException">If the specified type metadata belongs to a dataspace other than the dataspace of the command tree</exception>
        internal static void CheckType(TypeUsage type)
        {
            CheckType(type, "type");
        }

        internal static void CheckType(TypeUsage type, string varName)
        {
            CheckReadOnly(type, varName);

            // TypeUsage constructor is responsible for basic validation of EdmType
            Debug.Assert(type.EdmType != null, "TypeUsage constructor allowed null EdmType?");

            if (!CheckDataSpace(type))
            {
                throw new ArgumentException(Strings.Cqt_Metadata_TypeUsageIncorrectSpace, "type");
            }
        }

        // <summary>
        // Verifies that the specified member is valid - non-null, from the same metadata workspace and data space as the command tree, etc
        // </summary>
        // <param name="memberMeta"> The member to verify </param>
        // <param name="varName"> The name of the variable to which this member instance is being assigned </param>
        internal static void CheckMember(EdmMember memberMeta, string varName)
        {
            DebugCheck.NotNull(memberMeta);
            CheckReadOnly(memberMeta.DeclaringType, varName);

            // EdmMember constructor is responsible for basic validation
            Debug.Assert(memberMeta.Name != null, "EdmMember constructor allowed null name?");
            Debug.Assert(null != memberMeta.TypeUsage, "EdmMember constructor allowed null for TypeUsage?");
            Debug.Assert(null != memberMeta.DeclaringType, "EdmMember constructor allowed null for DeclaringType?");
            if (!CheckDataSpace(memberMeta.TypeUsage)
                || !CheckDataSpace(memberMeta.DeclaringType))
            {
                throw new ArgumentException(Strings.Cqt_Metadata_EdmMemberIncorrectSpace, varName);
            }
        }

        private static void CheckParameter(FunctionParameter paramMeta, string varName)
        {
            DebugCheck.NotNull(paramMeta);
            CheckReadOnly(paramMeta.DeclaringFunction, varName);

            // FunctionParameter constructor is responsible for basic validation
            Debug.Assert(paramMeta.Name != null, "FunctionParameter constructor allowed null name?");

            // Verify that the parameter is from the same workspace as the DbCommandTree
            if (!CheckDataSpace(paramMeta.TypeUsage))
            {
                throw new ArgumentException(Strings.Cqt_Metadata_FunctionParameterIncorrectSpace, varName);
            }
        }

        // <summary>
        // Verifies that the specified function metadata is valid - non-null and either created by this command tree (if a LambdaFunction) or from the same metadata collection and data space as the command tree (for ordinary function metadata)
        // </summary>
        // <param name="function"> The function metadata to verify </param>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        private static void CheckFunction(EdmFunction function)
        {
            CheckReadOnly(function, "function");

            Debug.Assert(function.Name != null, "EdmType constructor allowed null name?");

            if (!CheckDataSpace(function))
            {
                throw new ArgumentException(Strings.Cqt_Metadata_FunctionIncorrectSpace, "function");
            }

            // Composable functions must have a return parameter.
            if (function.IsComposableAttribute
                && null == function.ReturnParameter)
            {
                throw new ArgumentException(Strings.Cqt_Metadata_FunctionReturnParameterNull, "function");
            }

            // Verify that the function ReturnType - if present - is from the DbCommandTree's metadata collection and dataspace
            // A return parameter is not required for non-composable functions.
            if (function.ReturnParameter != null)
            {
                if (!CheckDataSpace(function.ReturnParameter.TypeUsage))
                {
                    throw new ArgumentException(Strings.Cqt_Metadata_FunctionParameterIncorrectSpace, "function.ReturnParameter");
                }
            }

            // Verify that the function parameters collection is non-null and,
            // if non-empty, contains valid IParameterMetadata instances.
            IList<FunctionParameter> functionParams = function.Parameters;
            Debug.Assert(functionParams != null, "EdmFunction constructor did not initialize Parameters?");

            for (var idx = 0; idx < functionParams.Count; idx++)
            {
                CheckParameter(functionParams[idx], StringUtil.FormatIndex("function.Parameters", idx));
            }
        }

        // <summary>
        // Verifies that the specified EntitySet is valid with respect to the command tree
        // </summary>
        // <param name="entitySet"> The EntitySet to verify </param>
        // <param name="varName"> The variable name to use if an exception should be thrown </param>
        internal static void CheckEntitySet(EntitySetBase entitySet, string varName)
        {
            CheckReadOnly(entitySet, varName);

            // EntitySetBase constructor is responsible for basic validation of set name and element type
            Debug.Assert(!string.IsNullOrEmpty(entitySet.Name), "EntitySetBase constructor allowed null/empty set name?");

            // Verify the Extent's Container
            if (null == entitySet.EntityContainer)
            {
                throw new ArgumentException(Strings.Cqt_Metadata_EntitySetEntityContainerNull, varName);
            }

            if (!CheckDataSpace(entitySet.EntityContainer))
            {
                throw new ArgumentException(Strings.Cqt_Metadata_EntitySetIncorrectSpace, varName);
            }

            // Verify the Extent's Entity Type
            // EntitySetBase constructor is responsible for basic validation of set name and element type
            Debug.Assert(entitySet.ElementType != null, "EntitySetBase constructor allowed null container?");

            if (!CheckDataSpace(entitySet.ElementType))
            {
                throw new ArgumentException(Strings.Cqt_Metadata_EntitySetIncorrectSpace, varName);
            }
        }

        private static bool CheckDataSpace(TypeUsage type)
        {
            return CheckDataSpace(type.EdmType);
        }

        private static bool CheckDataSpace(GlobalItem item)
        {
            // Since the set of primitive types and canonical functions are shared, we don't need to check for them.
            // Additionally, any non-canonical function in the C-Space must be a cached store function, which will
            // also not be present in the workspace.
            if (BuiltInTypeKind.PrimitiveType == item.BuiltInTypeKind
                ||
                (BuiltInTypeKind.EdmFunction == item.BuiltInTypeKind && DataSpace.CSpace == item.DataSpace))
            {
                return true;
            }

            // Transient types should be checked according to their non-transient element types
            if (Helper.IsRowType(item))
            {
                foreach (var prop in ((RowType)item).Properties)
                {
                    if (!CheckDataSpace(prop.TypeUsage))
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (Helper.IsCollectionType(item))
            {
                return CheckDataSpace(((CollectionType)item).TypeUsage);
            }
            else if (Helper.IsRefType(item))
            {
                return CheckDataSpace(((RefType)item).ElementType);
            }
            else
            {
                return (item.DataSpace == DataSpace.SSpace || item.DataSpace == DataSpace.CSpace);
            }
        }

        internal static TypeUsage CreateCollectionOfRowResultType(List<KeyValuePair<string, TypeUsage>> columns)
        {
            var retUsage = TypeUsage.Create(
                TypeHelpers.CreateCollectionType(
                    TypeUsage.Create(
                        TypeHelpers.CreateRowType(columns)
                        )
                    )
                );

            return retUsage;
        }

        private static TypeUsage CreateResultType(EdmType resultType)
        {
            return TypeUsage.Create(resultType);
        }

        private static TypeUsage CreateResultType(RelationshipEndMember end)
        {
            var retType = end.TypeUsage;
            if (!TypeSemantics.IsReferenceType(retType))
            {
                // The only relation end that is currently allowed to have a non-Reference type is the Child end of
                // a composition, in which case the end type must be an entity type. 
                //
                ////Debug.Assert(end.Relation.IsComposition && !end.IsParent && (end.Type is EntityType), "Relation end can only have non-Reference type if it is a Composition child end");

                retType = TypeHelpers.CreateReferenceTypeUsage(TypeHelpers.GetEdmType<EntityType>(retType));
            }

            // If the upper bound is not 1 the result type is a collection of the given type
            if (RelationshipMultiplicity.Many
                == end.RelationshipMultiplicity)
            {
                retType = TypeHelpers.CreateCollectionTypeUsage(retType);
            }

            return retType;
        }

        internal static TypeUsage CreateReferenceResultType(EntityTypeBase referencedEntityType)
        {
            return TypeUsage.Create(TypeHelpers.CreateReferenceType(referencedEntityType));
        }

        private static bool TryGetPrimitiveTypeKind(Type clrType, out PrimitiveTypeKind primitiveTypeKind)
        {
            return ClrProviderManifest.TryGetPrimitiveTypeKind(clrType, out primitiveTypeKind);
        }

        // <summary>
        // Checks whether the clr enum type matched the edm enum type.
        // </summary>
        // <param name="edmEnumType"> Edm enum type. </param>
        // <param name="clrEnumType"> Clr enum type. </param>
        // <returns>
        // <c>true</c> if types match otherwise <c>false</c> .
        // </returns>
        // <remarks>
        // The clr enum type matches the edm enum type if:
        // - type names are the same
        // - both types have the same underlying type (note that this prevents from over- and underflows)
        // - the edm enum type does not have more members than the clr enum type
        // - members have the same names
        // - members have the same values
        // </remarks>
        private static bool ClrEdmEnumTypesMatch(EnumType edmEnumType, Type clrEnumType)
        {
            DebugCheck.NotNull(edmEnumType);
            DebugCheck.NotNull(clrEnumType);
            Debug.Assert(clrEnumType.IsEnum(), "non enum clr type.");

            // check that type names are the same and the edm type does not have more members than the clr type.
            if (clrEnumType.Name != edmEnumType.Name
                || clrEnumType.GetEnumNames().Length < edmEnumType.Members.Count)
            {
                return false;
            }

            // check that both types have the same underlying type (note that this also prevents from over- and underflows)
            PrimitiveTypeKind clrEnumUnderlyingTypeKind;
            if (!TryGetPrimitiveTypeKind(clrEnumType.GetEnumUnderlyingType(), out clrEnumUnderlyingTypeKind)
                || clrEnumUnderlyingTypeKind != edmEnumType.UnderlyingType.PrimitiveTypeKind)
            {
                return false;
            }

            // check that all the members have the same names and values
            foreach (var edmEnumTypeMember in edmEnumType.Members)
            {
                Debug.Assert(
                    edmEnumTypeMember.Value.GetType() == clrEnumType.GetEnumUnderlyingType(),
                    "Enum underlying types matched so types of member values must match the enum underlying type as well");

                if (!clrEnumType.GetEnumNames().Contains(edmEnumTypeMember.Name)
                    || !edmEnumTypeMember.Value.Equals(
                        Convert.ChangeType(
                            Enum.Parse(clrEnumType, edmEnumTypeMember.Name), clrEnumType.GetEnumUnderlyingType(),
                            CultureInfo.InvariantCulture)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
