// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;

    internal class MslXmlSchemaWriter : XmlSchemaWriter
    {
        private string _entityTypeNamespace;
        private string _dbSchemaName;

        internal MslXmlSchemaWriter(XmlWriter xmlWriter, double version)
        {
            DebugCheck.NotNull(xmlWriter);

            _xmlWriter = xmlWriter;
            _version = version;
        }

        internal void WriteSchema(DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(databaseMapping);

            WriteSchemaElementHeader();
            WriteDbModelElement(databaseMapping);
            WriteEndElement();
        }

        private void WriteSchemaElementHeader()
        {
            var xmlNamespace = MslConstructs.GetMslNamespace(_version);
            _xmlWriter.WriteStartElement(MslConstructs.MappingElement, xmlNamespace);
            _xmlWriter.WriteAttributeString(MslConstructs.MappingSpaceAttribute, "C-S");
        }

        private void WriteDbModelElement(DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(databaseMapping);

            _entityTypeNamespace = databaseMapping.Model.NamespaceNames.SingleOrDefault();
            _dbSchemaName = databaseMapping.Database.Containers.Single().Name;

            WriteEntityContainerMappingElement(databaseMapping.EntityContainerMappings.First());
        }

        // internal for testing
        internal void WriteEntityContainerMappingElement(EntityContainerMapping containerMapping)
        {
            DebugCheck.NotNull(containerMapping);

            _xmlWriter.WriteStartElement(MslConstructs.EntityContainerMappingElement);
            _xmlWriter.WriteAttributeString(MslConstructs.StorageEntityContainerAttribute, _dbSchemaName);
            _xmlWriter.WriteAttributeString(
                MslConstructs.CdmEntityContainerAttribute, containerMapping.EdmEntityContainer.Name);

            foreach (var set in containerMapping.EntitySetMappings)
            {
                WriteEntitySetMappingElement(set);
            }

            foreach (var set in containerMapping.AssociationSetMappings)
            {
                WriteAssociationSetMappingElement(set);
            }

            foreach (var functionMapping in containerMapping.FunctionImportMappings.OfType<FunctionImportMappingComposable>())
            {
                WriteFunctionImportMappingElement(functionMapping);
            }

            foreach (var functionMapping in containerMapping.FunctionImportMappings.OfType<FunctionImportMappingNonComposable>())
            {
                WriteFunctionImportMappingElement(functionMapping);
            }

            _xmlWriter.WriteEndElement();
        }

        public void WriteEntitySetMappingElement(EntitySetMapping entitySetMapping)
        {
            DebugCheck.NotNull(entitySetMapping);

            _xmlWriter.WriteStartElement(MslConstructs.EntitySetMappingElement);
            _xmlWriter.WriteAttributeString(MslConstructs.EntitySetMappingNameAttribute, entitySetMapping.EntitySet.Name);

            foreach (var entityTypeMapping in entitySetMapping.EntityTypeMappings)
            {
                WriteEntityTypeMappingElement(entityTypeMapping);
            }

            foreach (var modificationFunctionMapping in entitySetMapping.ModificationFunctionMappings)
            {
                _xmlWriter.WriteStartElement(MslConstructs.EntityTypeMappingElement);
                _xmlWriter.WriteAttributeString(
                    MslConstructs.EntityTypeMappingTypeNameAttribute,
                    GetEntityTypeName(_entityTypeNamespace + "." + modificationFunctionMapping.EntityType.Name, false));

                WriteModificationFunctionMapping(modificationFunctionMapping);

                _xmlWriter.WriteEndElement();
            }

            _xmlWriter.WriteEndElement();
        }

        public void WriteAssociationSetMappingElement(AssociationSetMapping associationSetMapping)
        {
            DebugCheck.NotNull(associationSetMapping);

            _xmlWriter.WriteStartElement(MslConstructs.AssociationSetMappingElement);
            _xmlWriter.WriteAttributeString(
                MslConstructs.AssociationSetMappingNameAttribute, associationSetMapping.AssociationSet.Name);
            _xmlWriter.WriteAttributeString(
                MslConstructs.AssociationSetMappingTypeNameAttribute,
                _entityTypeNamespace + "." + associationSetMapping.AssociationSet.ElementType.Name);
            _xmlWriter.WriteAttributeString(
                MslConstructs.AssociationSetMappingStoreEntitySetAttribute, associationSetMapping.Table.Name);

            WriteAssociationEndMappingElement(associationSetMapping.SourceEndMapping);
            WriteAssociationEndMappingElement(associationSetMapping.TargetEndMapping);

            if (associationSetMapping.ModificationFunctionMapping != null)
            {
                WriteModificationFunctionMapping(associationSetMapping.ModificationFunctionMapping);
            }

            foreach (var conditionColumn in associationSetMapping.Conditions)
            {
                WriteConditionElement(conditionColumn);
            }

            _xmlWriter.WriteEndElement();
        }

        private void WriteAssociationEndMappingElement(EndPropertyMapping endMapping)
        {
            DebugCheck.NotNull(endMapping);

            _xmlWriter.WriteStartElement(MslConstructs.EndPropertyMappingElement);
            _xmlWriter.WriteAttributeString(MslConstructs.EndPropertyMappingNameAttribute, endMapping.AssociationEnd.Name);

            foreach (var propertyMapping in endMapping.PropertyMappings)
            {
                WriteScalarPropertyElement(
                    propertyMapping.Property.Name,
                    propertyMapping.Column.Name);
            }

            _xmlWriter.WriteEndElement();
        }

        private void WriteEntityTypeMappingElement(EntityTypeMapping entityTypeMapping)
        {
            DebugCheck.NotNull(entityTypeMapping);

            _xmlWriter.WriteStartElement(MslConstructs.EntityTypeMappingElement);
            _xmlWriter.WriteAttributeString(
                MslConstructs.EntityTypeMappingTypeNameAttribute,
                GetEntityTypeName(
                    _entityTypeNamespace + "." + entityTypeMapping.EntityType.Name, entityTypeMapping.IsHierarchyMapping));

            foreach (var mappingFragment in entityTypeMapping.MappingFragments)
            {
                WriteMappingFragmentElement(mappingFragment);
            }

            _xmlWriter.WriteEndElement();
        }

        internal void WriteMappingFragmentElement(MappingFragment mappingFragment)
        {
            DebugCheck.NotNull(mappingFragment);

            _xmlWriter.WriteStartElement(MslConstructs.MappingFragmentElement);

            _xmlWriter.WriteAttributeString(
                MslConstructs.MappingFragmentStoreEntitySetAttribute,
                mappingFragment.TableSet.Name);

            foreach (var propertyMapping in mappingFragment.PropertyMappings)
            {
                WritePropertyMapping(propertyMapping);
            }

            foreach (var conditionColumn in mappingFragment.ColumnConditions)
            {
                WriteConditionElement(conditionColumn);
            }

            _xmlWriter.WriteEndElement();
        }

        public void WriteFunctionImportMappingElement(FunctionImportMappingComposable functionImportMapping)
        {
            DebugCheck.NotNull(functionImportMapping);

            WriteFunctionImportMappingStartElement(functionImportMapping);

            // no mapping written when mapping to a scalar
            if (functionImportMapping.StructuralTypeMappings != null)
            {
                _xmlWriter.WriteStartElement(MslConstructs.FunctionImportMappingResultMapping);

                Debug.Assert(
                    functionImportMapping.StructuralTypeMappings.Count == 1,
                    "multiple result sets not supported.");

                var structuralMapping = functionImportMapping.StructuralTypeMappings.Single();

                if (structuralMapping.Item1.BuiltInTypeKind == BuiltInTypeKind.ComplexType)
                {
                    _xmlWriter.WriteStartElement(MslConstructs.ComplexTypeMappingElement);
                    _xmlWriter.WriteAttributeString(MslConstructs.ComplexTypeMappingTypeNameAttribute, structuralMapping.Item1.FullName);
                }
                else
                {
                    Debug.Assert(structuralMapping.Item1.BuiltInTypeKind == BuiltInTypeKind.EntityType, "Unexpected return type");

                    _xmlWriter.WriteStartElement(MslConstructs.EntityTypeMappingElement);
                    _xmlWriter.WriteAttributeString(MslConstructs.EntityTypeMappingTypeNameAttribute, structuralMapping.Item1.FullName);

                    foreach (var conditionMapping in structuralMapping.Item2)
                    {
                        WriteConditionElement(conditionMapping);
                    }
                }

                foreach (var propertyMapping in structuralMapping.Item3)
                {
                    WritePropertyMapping(propertyMapping);
                }

                _xmlWriter.WriteEndElement();
                _xmlWriter.WriteEndElement();
            }

            WriteFunctionImportEndElement();
        }

        public void WriteFunctionImportMappingElement(FunctionImportMappingNonComposable functionImportMapping)
        {
            DebugCheck.NotNull(functionImportMapping);

            WriteFunctionImportMappingStartElement(functionImportMapping);

            foreach (var resultMapping in functionImportMapping.ResultMappings)
            {
                WriteFunctionImportResultMappingElement(resultMapping);
            }

            WriteFunctionImportEndElement();
        }

        private void WriteFunctionImportMappingStartElement(FunctionImportMapping functionImportMapping)
        {
            _xmlWriter.WriteStartElement(MslConstructs.FunctionImportMappingElement);
            _xmlWriter.WriteAttributeString(
                MslConstructs.FunctionImportMappingFunctionNameAttribute,
                functionImportMapping.TargetFunction.FullName);
            _xmlWriter.WriteAttributeString(
                MslConstructs.FunctionImportMappingFunctionImportNameAttribute,
                functionImportMapping.FunctionImport.Name);
        }

        private void WriteFunctionImportResultMappingElement(FunctionImportResultMapping resultMapping)
        {
            DebugCheck.NotNull(resultMapping);
            _xmlWriter.WriteStartElement(MslConstructs.FunctionImportMappingResultMapping);

            foreach (var typeMapping in resultMapping.TypeMappings)
            {
                var entityTypeMapping = typeMapping as FunctionImportEntityTypeMapping;
                if (entityTypeMapping != null)
                {
                    WriteFunctionImportEntityTypeMappingElement(entityTypeMapping);
                }
                else
                {
                    WriteFunctionImportComplexTypeMappingElement((FunctionImportComplexTypeMapping)typeMapping);
                }
            }

            _xmlWriter.WriteEndElement();
        }

        private void WriteFunctionImportEntityTypeMappingElement(FunctionImportEntityTypeMapping entityTypeMapping)
        {
            DebugCheck.NotNull(entityTypeMapping);

            _xmlWriter.WriteStartElement(MslConstructs.EntityTypeMappingElement);

            var entityTypeName = CreateFunctionImportEntityTypeMappingTypeName(entityTypeMapping);

            _xmlWriter.WriteAttributeString(MslConstructs.EntityTypeMappingTypeNameAttribute, entityTypeName);

            WriteFunctionImportPropertyMappingElements(
                entityTypeMapping.PropertyMappings.Cast<FunctionImportReturnTypeScalarPropertyMapping>());

            foreach (var condition in entityTypeMapping.Conditions)
            {
                WriteFunctionImportConditionElement(condition);
            }

            _xmlWriter.WriteEndElement();
        }

        // internal for testing
        internal static string CreateFunctionImportEntityTypeMappingTypeName(FunctionImportEntityTypeMapping entityTypeMapping)
        {
            var entityTypeName =
                string.Join(
                    ";",
                    entityTypeMapping.EntityTypes.Select(e => GetEntityTypeName(e.FullName, false))
                        .Concat(entityTypeMapping.IsOfTypeEntityTypes.Select(e => GetEntityTypeName(e.FullName, true))));

            return entityTypeName;
        }

        private void WriteFunctionImportComplexTypeMappingElement(FunctionImportComplexTypeMapping complexTypeMapping)
        {
            DebugCheck.NotNull(complexTypeMapping);

            _xmlWriter.WriteStartElement(MslConstructs.ComplexTypeMappingElement);
            _xmlWriter.WriteAttributeString(MslConstructs.ComplexTypeMappingTypeNameAttribute, complexTypeMapping.ReturnType.FullName);

            WriteFunctionImportPropertyMappingElements(
                complexTypeMapping.PropertyMappings.Cast<FunctionImportReturnTypeScalarPropertyMapping>());

            _xmlWriter.WriteEndElement();
        }

        private void WriteFunctionImportPropertyMappingElements(IEnumerable<FunctionImportReturnTypeScalarPropertyMapping> propertyMappings)
        {
            foreach (var propertyMapping in propertyMappings)
            {
                WriteScalarPropertyElement(propertyMapping.PropertyName, propertyMapping.ColumnName);
            }            
        }

        private void WriteFunctionImportConditionElement(FunctionImportEntityTypeMappingCondition condition)
        {
            DebugCheck.NotNull(condition);
            _xmlWriter.WriteStartElement(MslConstructs.ConditionElement);
            _xmlWriter.WriteAttributeString(MslConstructs.ConditionColumnNameAttribute, condition.ColumnName);

            var isNullCondition = condition as FunctionImportEntityTypeMappingConditionIsNull;
            if (isNullCondition != null)
            {
                WriteIsNullConditionAttribute(isNullCondition.IsNull);
            }
            else
            {
                WriteConditionValue(((FunctionImportEntityTypeMappingConditionValue)condition).Value);
            }

            _xmlWriter.WriteEndElement();
        }

        private void WriteFunctionImportEndElement()
        {
            _xmlWriter.WriteEndElement();
        }

        private void WriteModificationFunctionMapping(EntityTypeModificationFunctionMapping modificationFunctionMapping)
        {
            DebugCheck.NotNull(modificationFunctionMapping);

            _xmlWriter.WriteStartElement(MslConstructs.ModificationFunctionMappingElement);

            WriteFunctionMapping(MslConstructs.InsertFunctionElement, modificationFunctionMapping.InsertFunctionMapping);
            WriteFunctionMapping(MslConstructs.UpdateFunctionElement, modificationFunctionMapping.UpdateFunctionMapping);
            WriteFunctionMapping(MslConstructs.DeleteFunctionElement, modificationFunctionMapping.DeleteFunctionMapping);

            _xmlWriter.WriteEndElement();
        }

        private void WriteModificationFunctionMapping(AssociationSetModificationFunctionMapping modificationFunctionMapping)
        {
            DebugCheck.NotNull(modificationFunctionMapping);

            _xmlWriter.WriteStartElement(MslConstructs.ModificationFunctionMappingElement);

            WriteFunctionMapping(
                MslConstructs.InsertFunctionElement,
                modificationFunctionMapping.InsertFunctionMapping,
                associationSetMapping: true);

            WriteFunctionMapping(
                MslConstructs.DeleteFunctionElement,
                modificationFunctionMapping.DeleteFunctionMapping,
                associationSetMapping: true);

            _xmlWriter.WriteEndElement();
        }

        public void WriteFunctionMapping(
            string functionElement, ModificationFunctionMapping functionMapping, bool associationSetMapping = false)
        {
            DebugCheck.NotNull(functionMapping);

            _xmlWriter.WriteStartElement(functionElement);
            _xmlWriter.WriteAttributeString(MslConstructs.FunctionNameAttribute, functionMapping.Function.FullName);

            if (functionMapping.RowsAffectedParameter != null)
            {
                _xmlWriter.WriteAttributeString(
                    MslConstructs.RowsAffectedParameterAttribute,
                    functionMapping.RowsAffectedParameter.Name);
            }

            if (!associationSetMapping)
            {
                WritePropertyParameterBindings(functionMapping.ParameterBindings);
                WriteAssociationParameterBindings(functionMapping.ParameterBindings);

                if (functionMapping.ResultBindings != null)
                {
                    WriteResultBindings(functionMapping.ResultBindings);
                }
            }
            else
            {
                WriteAssociationSetMappingParameterBindings(functionMapping.ParameterBindings);
            }

            _xmlWriter.WriteEndElement();
        }

        private void WriteAssociationSetMappingParameterBindings(
            IEnumerable<ModificationFunctionParameterBinding> parameterBindings)
        {
            DebugCheck.NotNull(parameterBindings);

            var propertyGroups
                = from pm in parameterBindings
                  where pm.MemberPath.AssociationSetEnd != null
                  group pm by pm.MemberPath.AssociationSetEnd;

            foreach (var group in propertyGroups)
            {
                _xmlWriter.WriteStartElement(MslConstructs.EndPropertyMappingElement);
                _xmlWriter.WriteAttributeString(MslConstructs.EndPropertyMappingNameAttribute, group.Key.Name);

                foreach (var functionParameterBinding in group)
                {
                    WriteScalarParameterElement(functionParameterBinding.MemberPath.Members.First(), functionParameterBinding);
                }

                _xmlWriter.WriteEndElement();
            }
        }

        private void WritePropertyParameterBindings(
            IEnumerable<ModificationFunctionParameterBinding> parameterBindings, int level = 0)
        {
            DebugCheck.NotNull(parameterBindings);

            var propertyGroups
                = from pm in parameterBindings
                  where pm.MemberPath.AssociationSetEnd == null
                        && pm.MemberPath.Members.Count() > level
                  group pm by pm.MemberPath.Members.ElementAt(level);

            foreach (var group in propertyGroups)
            {
                var property = (EdmProperty)group.Key;

                if (property.IsComplexType)
                {
                    _xmlWriter.WriteStartElement(MslConstructs.ComplexPropertyElement);
                    _xmlWriter.WriteAttributeString(MslConstructs.ComplexPropertyNameAttribute, property.Name);
                    _xmlWriter.WriteAttributeString(
                        MslConstructs.ComplexPropertyTypeNameAttribute,
                        _entityTypeNamespace + "." + property.ComplexType.Name);

                    WritePropertyParameterBindings(group, level + 1);

                    _xmlWriter.WriteEndElement();
                }
                else
                {
                    foreach (var parameterBinding in group)
                    {
                        WriteScalarParameterElement(property, parameterBinding);
                    }
                }
            }
        }

        private void WriteAssociationParameterBindings(
            IEnumerable<ModificationFunctionParameterBinding> parameterBindings)
        {
            DebugCheck.NotNull(parameterBindings);

            var propertyGroups
                = from pm in parameterBindings
                  where pm.MemberPath.AssociationSetEnd != null
                  group pm by pm.MemberPath.AssociationSetEnd;

            foreach (var group in propertyGroups)
            {
                _xmlWriter.WriteStartElement(MslConstructs.AssociationEndElement);

                var assocationSet = group.Key.ParentAssociationSet;

                _xmlWriter.WriteAttributeString(MslConstructs.AssociationSetAttribute, assocationSet.Name);
                _xmlWriter.WriteAttributeString(MslConstructs.FromAttribute, group.Key.Name);
                _xmlWriter.WriteAttributeString(
                    MslConstructs.ToAttribute,
                    assocationSet.AssociationSetEnds.Single(ae => ae != group.Key).Name);

                foreach (var functionParameterBinding in group)
                {
                    WriteScalarParameterElement(functionParameterBinding.MemberPath.Members.First(), functionParameterBinding);
                }

                _xmlWriter.WriteEndElement();
            }
        }

        private void WriteResultBindings(IEnumerable<ModificationFunctionResultBinding> resultBindings)
        {
            DebugCheck.NotNull(resultBindings);

            foreach (var resultBinding in resultBindings)
            {
                _xmlWriter.WriteStartElement(MslConstructs.ResultBindingElement);
                _xmlWriter.WriteAttributeString(MslConstructs.ScalarPropertyNameAttribute, resultBinding.Property.Name);
                _xmlWriter.WriteAttributeString(MslConstructs.ScalarPropertyColumnNameAttribute, resultBinding.ColumnName);
                _xmlWriter.WriteEndElement();
            }
        }

        private void WriteScalarParameterElement(EdmMember member, ModificationFunctionParameterBinding parameterBinding)
        {
            DebugCheck.NotNull(member);
            DebugCheck.NotNull(parameterBinding);

            _xmlWriter.WriteStartElement(MslConstructs.ScalarPropertyElement);
            _xmlWriter.WriteAttributeString(MslConstructs.ScalarPropertyNameAttribute, member.Name);
            _xmlWriter.WriteAttributeString(MslConstructs.ParameterNameAttribute, parameterBinding.Parameter.Name);
            _xmlWriter.WriteAttributeString(
                MslConstructs.ParameterVersionAttribute,
                parameterBinding.IsCurrent
                    ? MslConstructs.ParameterVersionAttributeCurrentValue
                    : MslConstructs.ParameterVersionAttributeOriginalValue);
            _xmlWriter.WriteEndElement();
        }

        private void WritePropertyMapping(PropertyMapping propertyMapping)
        {
            DebugCheck.NotNull(propertyMapping);

            var scalarPropertyMapping = propertyMapping as ScalarPropertyMapping;

            if (scalarPropertyMapping != null)
            {
                WritePropertyMapping(scalarPropertyMapping);
            }
            else
            {
                var complexPropertyMapping = propertyMapping as ComplexPropertyMapping;

                if (complexPropertyMapping != null)
                {
                    WritePropertyMapping(complexPropertyMapping);
                }
            }
        }

        private void WritePropertyMapping(ScalarPropertyMapping scalarPropertyMapping)
        {
            DebugCheck.NotNull(scalarPropertyMapping);

            WriteScalarPropertyElement(scalarPropertyMapping.Property.Name, scalarPropertyMapping.Column.Name);
        }

        private void WritePropertyMapping(ComplexPropertyMapping complexPropertyMapping)
        {
            DebugCheck.NotNull(complexPropertyMapping);

            _xmlWriter.WriteStartElement(MslConstructs.ComplexPropertyElement);
            _xmlWriter.WriteAttributeString(MslConstructs.ComplexPropertyNameAttribute, complexPropertyMapping.Property.Name);
            _xmlWriter.WriteAttributeString(
                MslConstructs.ComplexPropertyTypeNameAttribute,
                _entityTypeNamespace + "." + complexPropertyMapping.Property.ComplexType.Name);

            foreach (var propertyMapping in complexPropertyMapping.TypeMappings.Single().PropertyMappings)
            {
                WritePropertyMapping(propertyMapping);
            }

            _xmlWriter.WriteEndElement();
        }

        private static string GetEntityTypeName(string fullyQualifiedEntityTypeName, bool isHierarchyMapping)
        {
            DebugCheck.NotEmpty(fullyQualifiedEntityTypeName);

            if (isHierarchyMapping)
            {
                return MslConstructs.IsTypeOf + fullyQualifiedEntityTypeName + MslConstructs.IsTypeOfTerminal;
            }

            return fullyQualifiedEntityTypeName;
        }

        private void WriteConditionElement(ConditionPropertyMapping condition)
        {
            DebugCheck.NotNull(condition);

            _xmlWriter.WriteStartElement(MslConstructs.ConditionElement);
            if (condition.IsNull.HasValue)
            {
                WriteIsNullConditionAttribute(condition.IsNull.Value);
            }
            else
            {
                WriteConditionValue(condition.Value);
            }
            _xmlWriter.WriteAttributeString(MslConstructs.ConditionColumnNameAttribute, condition.Column.Name);
            _xmlWriter.WriteEndElement();
        }

        private void WriteIsNullConditionAttribute(bool isNullValue)
        {
            _xmlWriter.WriteAttributeString(
                MslConstructs.ConditionIsNullAttribute, GetLowerCaseStringFromBoolValue(isNullValue));
        }

        private void WriteConditionValue(object conditionValue)
        {
            if (conditionValue is bool)
            {
                _xmlWriter.WriteAttributeString(MslConstructs.ConditionValueAttribute, (bool)conditionValue ? "1" : "0");
            }
            else
            {
                _xmlWriter.WriteAttributeString(MslConstructs.ConditionValueAttribute, conditionValue.ToString());
            }
        }

        private void WriteScalarPropertyElement(string propertyName, string columnName)
        {
            DebugCheck.NotNull(propertyName);
            DebugCheck.NotNull(columnName);

            _xmlWriter.WriteStartElement(MslConstructs.ScalarPropertyElement);
            _xmlWriter.WriteAttributeString(MslConstructs.ScalarPropertyNameAttribute, propertyName);
            _xmlWriter.WriteAttributeString(MslConstructs.ScalarPropertyColumnNameAttribute, columnName);
            _xmlWriter.WriteEndElement();
        }
    }
}
