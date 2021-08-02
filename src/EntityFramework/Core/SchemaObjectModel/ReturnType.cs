﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Xml;

    internal class ReturnType : ModelFunctionTypeElement
    {
        private CollectionKind _collectionKind = CollectionKind.None;
        private bool _isRefType;
        private string _unresolvedEntitySet;
        private bool _entitySetPathDefined;
        private ModelFunctionTypeElement _typeSubElement;
        private EntityContainerEntitySet _entitySet;

        #region constructor

        internal ReturnType(Function parentElement)
            : base(parentElement)
        {
            _typeUsageBuilder = new TypeUsageBuilder(this);
        }

        #endregion

        #region Properties

        internal bool IsRefType
        {
            get { return _isRefType; }
        }

        internal CollectionKind CollectionKind
        {
            get { return _collectionKind; }
        }

        internal EntityContainerEntitySet EntitySet
        {
            get { return _entitySet; }
        }

        internal bool EntitySetPathDefined
        {
            get { return _entitySetPathDefined; }
        }

        internal ModelFunctionTypeElement SubElement
        {
            get { return _typeSubElement; }
        }

        internal override TypeUsage TypeUsage
        {
            get
            {
                if (_typeSubElement != null)
                {
                    return _typeSubElement.GetTypeUsage();
                }
                else if (_typeUsage != null)
                {
                    return _typeUsage;
                }
                else if (base.TypeUsage == null)
                {
                    return null;
                }
                else if (_collectionKind != CollectionKind.None)
                {
                    return TypeUsage.Create(new CollectionType(base.TypeUsage));
                }
                else
                {
                    return base.TypeUsage;
                }
            }
        }

        #endregion

        internal override SchemaElement Clone(SchemaElement parentElement)
        {
            var parameter = new ReturnType((Function)parentElement);
            parameter._type = _type;
            parameter.Name = Name;
            parameter._typeUsageBuilder = _typeUsageBuilder;
            parameter._unresolvedType = _unresolvedType;
            parameter._unresolvedEntitySet = _unresolvedEntitySet;
            parameter._entitySetPathDefined = _entitySetPathDefined;
            parameter._entitySet = _entitySet;
            return parameter;
        }

        protected override bool HandleAttribute(XmlReader reader)
        {
            if (base.HandleAttribute(reader))
            {
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.TypeElement))
            {
                HandleTypeAttribute(reader);
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.EntitySet))
            {
                HandleEntitySetAttribute(reader);
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.EntitySetPath))
            {
                HandleEntitySetPathAttribute(reader);
                return true;
            }
            else if (_typeUsageBuilder.HandleAttribute(reader))
            {
                return true;
            }

            return false;
        }

        internal bool ResolveNestedTypeNames(
            Converter.ConversionCache convertedItemCache, Dictionary<SchemaElement, GlobalItem> newGlobalItems)
        {
            Debug.Assert(_typeSubElement != null, "Nested type expected.");
            return _typeSubElement.ResolveNameAndSetTypeUsage(convertedItemCache, newGlobalItems);
        }

        #region Private Methods

        private void HandleTypeAttribute(XmlReader reader)
        {
            DebugCheck.NotNull(reader);
            Debug.Assert(UnresolvedType == null);

            string type;
            if (!Utils.GetString(Schema, reader, out type))
            {
                return;
            }
            TypeModifier typeModifier;

            Function.RemoveTypeModifier(ref type, out typeModifier, out _isRefType);

            switch (typeModifier)
            {
                case TypeModifier.Array:
                    _collectionKind = CollectionKind.Bag;
                    break;
                default:
                    Debug.Assert(
                        typeModifier == TypeModifier.None,
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "Type is not valid for property {0}: {1}. The modifier for the type cannot be used in this context.", FQName,
                            reader.Value));
                    break;
            }

            if (!Utils.ValidateDottedName(Schema, reader, type))
            {
                return;
            }

            UnresolvedType = type;
        }

        private void HandleEntitySetAttribute(XmlReader reader)
        {
            DebugCheck.NotNull(reader);
            string entitySetName;
            if (Utils.GetString(Schema, reader, out entitySetName))
            {
                _unresolvedEntitySet = entitySetName;
            }
        }

        private void HandleEntitySetPathAttribute(XmlReader reader)
        {
            DebugCheck.NotNull(reader);
            string entitySetPath;
            if (Utils.GetString(Schema, reader, out entitySetPath))
            {
                // EF does not support this EDM 3.0 attribute, we only use it for validation.
                _entitySetPathDefined = true;
            }
        }

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.CollectionType))
            {
                HandleCollectionTypeElement(reader);
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.ReferenceType))
            {
                HandleReferenceTypeElement(reader);
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.TypeRef))
            {
                HandleTypeRefElement(reader);
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.RowType))
            {
                HandleRowTypeElement(reader);
                return true;
            }

            return false;
        }

        protected void HandleCollectionTypeElement(XmlReader reader)
        {
            DebugCheck.NotNull(reader);

            var subElement = new CollectionTypeElement(this);
            subElement.Parse(reader);
            _typeSubElement = subElement;
        }

        protected void HandleReferenceTypeElement(XmlReader reader)
        {
            DebugCheck.NotNull(reader);

            var subElement = new ReferenceTypeElement(this);
            subElement.Parse(reader);
            _typeSubElement = subElement;
        }

        protected void HandleTypeRefElement(XmlReader reader)
        {
            DebugCheck.NotNull(reader);

            var subElement = new TypeRefElement(this);
            subElement.Parse(reader);
            _typeSubElement = subElement;
        }

        protected void HandleRowTypeElement(XmlReader reader)
        {
            DebugCheck.NotNull(reader);

            var subElement = new RowTypeElement(this);
            subElement.Parse(reader);
            _typeSubElement = subElement;
        }

        #endregion

        internal override void ResolveTopLevelNames()
        {
            // If type was defined as an attribute: <ReturnType Type="int"/>
            if (_unresolvedType != null)
            {
                base.ResolveTopLevelNames();
            }

            // If type was defined as a subelement: <ReturnType><CollectionType>...</CollectionType></ReturnType>
            if (_typeSubElement != null)
            {
                Debug.Assert(
                    !ParentElement.IsFunctionImport,
                    "FunctionImports can't have sub elements in their return types, so we should NEVER see them here");
                _typeSubElement.ResolveTopLevelNames();
            }

            if (ParentElement.IsFunctionImport
                && _unresolvedEntitySet != null)
            {
                ((FunctionImportElement)ParentElement).ResolveEntitySet(this, _unresolvedEntitySet, ref _entitySet);
            }
        }

        internal override void Validate()
        {
            base.Validate();

            ValidationHelper.ValidateTypeDeclaration(this, _type, _typeSubElement);
            ValidationHelper.ValidateFacets(this, _type, _typeUsageBuilder);
            if (_isRefType)
            {
                ValidationHelper.ValidateRefType(this, _type);
            }

            if (Schema.DataModel
                != SchemaDataModelOption.EntityDataModel)
            {
                Debug.Assert(
                    Schema.DataModel == SchemaDataModelOption.ProviderDataModel ||
                    Schema.DataModel == SchemaDataModelOption.ProviderManifestModel, "Unexpected data model");

                if (Schema.DataModel
                    == SchemaDataModelOption.ProviderManifestModel)
                {
                    // Only scalar return type is allowed for functions in provider manifest.
                    if (_type != null && (_type is ScalarType == false || _collectionKind != CollectionKind.None)
                        ||
                        _typeSubElement != null && _typeSubElement.Type is ScalarType == false)
                    {
                        var typeName = "";
                        if (_type != null)
                        {
                            typeName = Function.GetTypeNameForErrorMessage(_type, _collectionKind, _isRefType);
                        }
                        else if (_typeSubElement != null)
                        {
                            typeName = _typeSubElement.FQName;
                        }
                        AddError(
                            ErrorCode.FunctionWithNonEdmTypeNotSupported,
                            EdmSchemaErrorSeverity.Error,
                            this,
                            Strings.FunctionWithNonEdmPrimitiveTypeNotSupported(typeName, ParentElement.FQName));
                    }
                }
                else // SchemaDataModelOption.ProviderDataModel
                {
                    Debug.Assert(Schema.DataModel == SchemaDataModelOption.ProviderDataModel, "Unexpected data model");

                    // In SSDL, function may only return a primitive type or a collection of rows.
                    if (_type != null)
                    {
                        // It is not possible to define a collection of rows via a type attribute, hence any collection is not allowed.
                        if (_type is ScalarType == false
                            || _collectionKind != CollectionKind.None)
                        {
                            AddError(
                                ErrorCode.FunctionWithNonPrimitiveTypeNotSupported,
                                EdmSchemaErrorSeverity.Error,
                                this,
                                Strings.FunctionWithNonPrimitiveTypeNotSupported(
                                    _isRefType ? _unresolvedType : _type.FQName, ParentElement.FQName));
                        }
                    }
                    else if (_typeSubElement != null)
                    {
                        if (_typeSubElement.Type is ScalarType == false)
                        {
                            if (Schema.SchemaVersion
                                < XmlConstants.StoreVersionForV3)
                            {
                                // Before V3 provider model functions only supported scalar return types.
                                AddError(
                                    ErrorCode.FunctionWithNonPrimitiveTypeNotSupported,
                                    EdmSchemaErrorSeverity.Error,
                                    this,
                                    Strings.FunctionWithNonPrimitiveTypeNotSupported(_typeSubElement.FQName, ParentElement.FQName));
                            }
                            else
                            {
                                // Starting from V3, TVFs must return collection of rows and row props can be only primitive types.
                                // The "collection of rows" is the only option in SSDL function ReturnType subelement thus it's enforced on the XSD level,
                                // so we can assume it here. The only thing we need to check is the type of the row properties.
                                var collection = _typeSubElement as CollectionTypeElement;
                                Debug.Assert(collection != null, "Can't find <CollectionType> inside TVF <ReturnType> element");
                                if (collection != null)
                                {
                                    var row = collection.SubElement as RowTypeElement;
                                    Debug.Assert(row != null, "Can't find <RowType> inside TVF <ReturnType><CollectionType> element");
                                    if (row != null)
                                    {
                                        if (row.Properties.Any(p => !p.ValidateIsScalar()))
                                        {
                                            AddError(
                                                ErrorCode.TVFReturnTypeRowHasNonScalarProperty,
                                                EdmSchemaErrorSeverity.Error,
                                                this,
                                                Strings.TVFReturnTypeRowHasNonScalarProperty);
                                        }
                                    }
                                }
                            }
                        }
                        // else type is ScalarType which is supported in all version
                    }
                }
            }

            if (_typeSubElement != null)
            {
                _typeSubElement.Validate();
            }
        }

        internal override void WriteIdentity(StringBuilder builder)
        {
        }

        internal override TypeUsage GetTypeUsage()
        {
            return TypeUsage;
        }

        internal override bool ResolveNameAndSetTypeUsage(
            Converter.ConversionCache convertedItemCache, Dictionary<SchemaElement, GlobalItem> newGlobalItems)
        {
            Debug.Fail(
                "This method was not called from anywhere in the code before. If you got here you need to update this method and possibly ResolveNestedTypeNames()");

            return false;
        }
    }
}
