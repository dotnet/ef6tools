// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Xml;

    internal sealed class BooleanFacetDescriptionElement : FacetDescriptionElement
    {
        public BooleanFacetDescriptionElement(TypeElement type, string name)
            : base(type, name)
        {
        }

        public override EdmType FacetType
        {
            get { return MetadataItem.EdmProviderManifest.GetPrimitiveType(PrimitiveTypeKind.Boolean); }
        }

        /////////////////////////////////////////////////////////////////////
        // Attribute Handlers

        // <summary>
        // Handler for the Default attribute
        // </summary>
        // <param name="reader"> xml reader currently positioned at Default attribute </param>
        protected override void HandleDefaultAttribute(XmlReader reader)
        {
            var value = false;
            if (HandleBoolAttribute(reader, ref value))
            {
                DefaultValue = value;
            }
        }
    }
}
