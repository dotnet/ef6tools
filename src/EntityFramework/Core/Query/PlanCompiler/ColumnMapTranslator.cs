// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.PlanCompiler
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Query.InternalTrees;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    // <summary>
    // Delegate pattern that the ColumnMapTranslator uses to find its replacement
    // columnMaps.  Given a columnMap, return it's replacement.
    // </summary>
    internal delegate ColumnMap ColumnMapTranslatorTranslationDelegate(ColumnMap columnMap);

    // <summary>
    // ColumnMapTranslator visits the ColumnMap hierarchy and runs the translation delegate
    // you specify;  There are some static methods to perform common translations, but you
    // can bring your own translation if you desire.
    // This visitor only creates new ColumnMap objects when necessary; it attempts to
    // replace-in-place, except when that is not possible because the field is not
    // writable.
    // NOTE: over time, we should be able to modify the ColumnMaps to have more writable
    // fields;
    // </summary>
    internal class ColumnMapTranslator : ColumnMapVisitorWithResults<ColumnMap, ColumnMapTranslatorTranslationDelegate>
    {
        #region Constructors

        // <summary>
        // Singleton instance for the "public" methods to use;
        // </summary>
        private static readonly ColumnMapTranslator _instance = new ColumnMapTranslator();

        // <summary>
        // Constructor; no one should use this.
        // </summary>
        private ColumnMapTranslator()
        {
        }

        #endregion

        #region Visitor Helpers

        // <summary>
        // Returns the var to use in the copy, either the original or the
        // replacement.  Note that we will follow the chain of replacements, in
        // case the replacement was also replaced.
        // </summary>
        private static Var GetReplacementVar(Var originalVar, Dictionary<Var, Var> replacementVarMap)
        {
            // SQLBUDT #478509: Follow the chain of mapped vars, don't
            //                  just stop at the first one
            var replacementVar = originalVar;

            while (replacementVarMap.TryGetValue(replacementVar, out originalVar))
            {
                if (originalVar == replacementVar)
                {
                    break;
                }
                replacementVar = originalVar;
            }
            return replacementVar;
        }

        #endregion

        #region "Public" surface area

        // <summary>
        // Bring-Your-Own-Replacement-Delegate method.
        // </summary>
        internal static ColumnMap Translate(ColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            return columnMap.Accept(_instance, translationDelegate);
        }

        // <summary>
        // Replace VarRefColumnMaps with the specified ColumnMap replacement
        // </summary>
        internal static ColumnMap Translate(ColumnMap columnMapToTranslate, Dictionary<Var, ColumnMap> varToColumnMap)
        {
            var result = Translate(
                columnMapToTranslate,
                delegate(ColumnMap columnMap)
                {
                    var varRefColumnMap = columnMap as VarRefColumnMap;
                    if (null != varRefColumnMap)
                    {
                        if (varToColumnMap.TryGetValue(varRefColumnMap.Var, out columnMap))
                        {
                            // perform fixups; only allow name changes when the replacement isn't
                            // already named (and the original is named...)
                            if (!columnMap.IsNamed
                                && varRefColumnMap.IsNamed)
                            {
                                columnMap.Name = varRefColumnMap.Name;
                            }

                            // The type of the original column map may be enum and we need to preserve it type when replacing
                            // the column map. For more details see https://entityframework.codeplex.com/workitem/1686
                            if (Helper.IsEnumType(varRefColumnMap.Type.EdmType) && varRefColumnMap.Type.EdmType != columnMap.Type.EdmType)
                            {
                                Debug.Assert(
                                    Helper.GetUnderlyingEdmTypeForEnumType(varRefColumnMap.Type.EdmType) == columnMap.Type.EdmType);

                                columnMap.Type = varRefColumnMap.Type;
                            }
                        }
                        else
                        {
                            columnMap = varRefColumnMap;
                        }
                    }
                    return columnMap;
                }
                );
            return result;
        }

        // <summary>
        // Replace VarRefColumnMaps with new VarRefColumnMaps with the specified Var
        // </summary>
        internal static ColumnMap Translate(ColumnMap columnMapToTranslate, Dictionary<Var, Var> varToVarMap)
        {
            var result = Translate(
                columnMapToTranslate,
                delegate(ColumnMap columnMap)
                {
                    var varRefColumnMap = columnMap as VarRefColumnMap;
                    if (null != varRefColumnMap)
                    {
                        var replacementVar = GetReplacementVar(varRefColumnMap.Var, varToVarMap);
                        if (varRefColumnMap.Var != replacementVar)
                        {
                            columnMap = new VarRefColumnMap(varRefColumnMap.Type, varRefColumnMap.Name, replacementVar);
                        }
                    }
                    return columnMap;
                }
                );

            return result;
        }

        // <summary>
        // Replace VarRefColumnMaps with ScalarColumnMaps referring to the command and column
        // </summary>
        internal static ColumnMap Translate(ColumnMap columnMapToTranslate, Dictionary<Var, KeyValuePair<int, int>> varToCommandColumnMap)
        {
            var result = Translate(
                columnMapToTranslate,
                delegate(ColumnMap columnMap)
                {
                    var varRefColumnMap = columnMap as VarRefColumnMap;
                    if (null != varRefColumnMap)
                    {
                        KeyValuePair<int, int> commandAndColumn;

                        if (!varToCommandColumnMap.TryGetValue(varRefColumnMap.Var, out commandAndColumn))
                        {
                            throw EntityUtil.InternalError(EntityUtil.InternalErrorCode.UnknownVar, 1, varRefColumnMap.Var.Id);
                            // shouldn't have gotten here without having a resolveable var
                        }
                        columnMap = new ScalarColumnMap(
                            varRefColumnMap.Type, varRefColumnMap.Name, commandAndColumn.Key, commandAndColumn.Value);
                    }

                    // While we're at it, we ensure that all columnMaps are named; we wait
                    // until this point, because we don't want to assign names until after
                    // we've gone through the transformations; 
                    if (!columnMap.IsNamed)
                    {
                        columnMap.Name = ColumnMap.DefaultColumnName;
                    }
                    return columnMap;
                }
                );

            return result;
        }

        #endregion

        #region Visitor methods

        #region List handling

        // <summary>
        // List(ColumnMap)
        // </summary>
        private void VisitList<TResultType>(TResultType[] tList, ColumnMapTranslatorTranslationDelegate translationDelegate)
            where TResultType : ColumnMap
        {
            for (var i = 0; i < tList.Length; i++)
            {
                tList[i] = (TResultType)tList[i].Accept(this, translationDelegate);
            }
        }

        #endregion

        #region EntityIdentity handling

        // <summary>
        // DiscriminatedEntityIdentity
        // </summary>
        protected override EntityIdentity VisitEntityIdentity(
            DiscriminatedEntityIdentity entityIdentity, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newEntitySetColumnMap = entityIdentity.EntitySetColumnMap.Accept(this, translationDelegate);
            VisitList(entityIdentity.Keys, translationDelegate);

            if (newEntitySetColumnMap != entityIdentity.EntitySetColumnMap)
            {
                entityIdentity = new DiscriminatedEntityIdentity(
                    (SimpleColumnMap)newEntitySetColumnMap, entityIdentity.EntitySetMap, entityIdentity.Keys);
            }
            return entityIdentity;
        }

        // <summary>
        // SimpleEntityIdentity
        // </summary>
        protected override EntityIdentity VisitEntityIdentity(
            SimpleEntityIdentity entityIdentity, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            VisitList(entityIdentity.Keys, translationDelegate);
            return entityIdentity;
        }

        #endregion

        // <summary>
        // ComplexTypeColumnMap
        // </summary>
        internal override ColumnMap Visit(ComplexTypeColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newNullSentinel = columnMap.NullSentinel;
            if (null != newNullSentinel)
            {
                newNullSentinel = (SimpleColumnMap)translationDelegate(newNullSentinel);
            }

            VisitList(columnMap.Properties, translationDelegate);

            if (columnMap.NullSentinel != newNullSentinel)
            {
                columnMap = new ComplexTypeColumnMap(columnMap.Type, columnMap.Name, columnMap.Properties, newNullSentinel);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // DiscriminatedCollectionColumnMap
        // </summary>
        internal override ColumnMap Visit(
            DiscriminatedCollectionColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newDiscriminator = columnMap.Discriminator.Accept(this, translationDelegate);
            VisitList(columnMap.ForeignKeys, translationDelegate);
            VisitList(columnMap.Keys, translationDelegate);
            var newElement = columnMap.Element.Accept(this, translationDelegate);

            if (newDiscriminator != columnMap.Discriminator
                || newElement != columnMap.Element)
            {
                columnMap = new DiscriminatedCollectionColumnMap(
                    columnMap.Type, columnMap.Name, newElement, columnMap.Keys, columnMap.ForeignKeys, (SimpleColumnMap)newDiscriminator,
                    columnMap.DiscriminatorValue);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // EntityColumnMap
        // </summary>
        internal override ColumnMap Visit(EntityColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newEntityIdentity = VisitEntityIdentity(columnMap.EntityIdentity, translationDelegate);
            VisitList(columnMap.Properties, translationDelegate);

            if (newEntityIdentity != columnMap.EntityIdentity)
            {
                columnMap = new EntityColumnMap(columnMap.Type, columnMap.Name, columnMap.Properties, newEntityIdentity);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // SimplePolymorphicColumnMap
        // </summary>
        internal override ColumnMap Visit(SimplePolymorphicColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newTypeDiscriminator = columnMap.TypeDiscriminator.Accept(this, translationDelegate);

            // NOTE: we're using Copy-On-Write logic to avoid allocation if we don't 
            //       need to change things.
            var newTypeChoices = columnMap.TypeChoices;
            foreach (var kv in columnMap.TypeChoices)
            {
                var newTypeChoice = (TypedColumnMap)kv.Value.Accept(this, translationDelegate);

                if (newTypeChoice != kv.Value)
                {
                    if (newTypeChoices == columnMap.TypeChoices)
                    {
                        newTypeChoices = new Dictionary<object, TypedColumnMap>(columnMap.TypeChoices);
                    }
                    newTypeChoices[kv.Key] = newTypeChoice;
                }
            }
            VisitList(columnMap.Properties, translationDelegate);

            if (newTypeDiscriminator != columnMap.TypeDiscriminator
                || newTypeChoices != columnMap.TypeChoices)
            {
                columnMap = new SimplePolymorphicColumnMap(
                    columnMap.Type, columnMap.Name, columnMap.Properties, (SimpleColumnMap)newTypeDiscriminator, newTypeChoices);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // MultipleDiscriminatorPolymorphicColumnMap
        // </summary>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ColumnMapTranslator")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
            MessageId = "MultipleDiscriminatorPolymorphicColumnMap")]
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            MessageId = "System.Data.Entity.Core.Query.PlanCompiler.PlanCompiler.Assert(System.Boolean,System.String)")]
        internal override ColumnMap Visit(
            MultipleDiscriminatorPolymorphicColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            // At this time, we shouldn't ever see this type here; it's for SPROCS which don't use
            // the plan compiler.
            PlanCompiler.Assert(false, "unexpected MultipleDiscriminatorPolymorphicColumnMap in ColumnMapTranslator");
            return null;
        }

        // <summary>
        // RecordColumnMap
        // </summary>
        internal override ColumnMap Visit(RecordColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newNullSentinel = columnMap.NullSentinel;
            if (null != newNullSentinel)
            {
                newNullSentinel = (SimpleColumnMap)translationDelegate(newNullSentinel);
            }

            VisitList(columnMap.Properties, translationDelegate);

            if (columnMap.NullSentinel != newNullSentinel)
            {
                columnMap = new RecordColumnMap(columnMap.Type, columnMap.Name, columnMap.Properties, newNullSentinel);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // RefColumnMap
        // </summary>
        internal override ColumnMap Visit(RefColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            var newEntityIdentity = VisitEntityIdentity(columnMap.EntityIdentity, translationDelegate);

            if (newEntityIdentity != columnMap.EntityIdentity)
            {
                columnMap = new RefColumnMap(columnMap.Type, columnMap.Name, newEntityIdentity);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // ScalarColumnMap
        // </summary>
        internal override ColumnMap Visit(ScalarColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            return translationDelegate(columnMap);
        }

        // <summary>
        // SimpleCollectionColumnMap
        // </summary>
        internal override ColumnMap Visit(SimpleCollectionColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            VisitList(columnMap.ForeignKeys, translationDelegate);
            VisitList(columnMap.Keys, translationDelegate);
            var newElement = columnMap.Element.Accept(this, translationDelegate);

            if (newElement != columnMap.Element)
            {
                columnMap = new SimpleCollectionColumnMap(columnMap.Type, columnMap.Name, newElement, columnMap.Keys, columnMap.ForeignKeys);
            }
            return translationDelegate(columnMap);
        }

        // <summary>
        // VarRefColumnMap
        // </summary>
        internal override ColumnMap Visit(VarRefColumnMap columnMap, ColumnMapTranslatorTranslationDelegate translationDelegate)
        {
            return translationDelegate(columnMap);
        }

        #endregion
    }
}
