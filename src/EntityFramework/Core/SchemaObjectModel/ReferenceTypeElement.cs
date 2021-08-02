﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Text;
    using System.Xml;

    internal class ReferenceTypeElement : ModelFunctionTypeElement
    {
        #region constructor

        internal ReferenceTypeElement(SchemaElement parentElement)
            : base(parentElement)
        {
        }

        #endregion

        protected override bool HandleAttribute(XmlReader reader)
        {
            if (base.HandleAttribute(reader))
            {
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.TypeElement))
            {
                HandleTypeElementAttribute(reader);
                return true;
            }

            return false;
        }

        protected void HandleTypeElementAttribute(XmlReader reader)
        {
            Debug.Assert(reader != null);

            string type;
            if (!Utils.GetString(Schema, reader, out type))
            {
                return;
            }

            if (!Utils.ValidateDottedName(Schema, reader, type))
            {
                return;
            }

            _unresolvedType = type;
        }

        internal override void WriteIdentity(StringBuilder builder)
        {
            Debug.Assert(UnresolvedType != null && UnresolvedType.Trim().Length != 0);
            builder.Append("Ref(" + UnresolvedType + ")");
        }

        internal override TypeUsage GetTypeUsage()
        {
            return _typeUsage;
        }

        internal override bool ResolveNameAndSetTypeUsage(
            Converter.ConversionCache convertedItemCache, Dictionary<SchemaElement, GlobalItem> newGlobalItems)
        {
            if (_typeUsage == null)
            {
                Debug.Assert(!(_type is ScalarType));

                var edmType = (EdmType)Converter.LoadSchemaElement(_type, _type.Schema.ProviderManifest, convertedItemCache, newGlobalItems);
                var entityType = edmType as EntityType;

                Debug.Assert(entityType != null);

                var refType = new RefType(entityType);
                refType.AddMetadataProperties(OtherContent);
                _typeUsage = TypeUsage.Create(refType);
            }
            return true;
        }

        internal override void Validate()
        {
            base.Validate();

            ValidationHelper.ValidateRefType(this, _type);
        }
    }
}
