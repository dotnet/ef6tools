// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Xml;

    // <summary>
    // Represents an role element in referential constraint element.
    // </summary>
    internal sealed class ReferentialConstraintRoleElement : SchemaElement
    {
        private List<PropertyRefElement> _roleProperties;
        private IRelationshipEnd _end;

        // <summary>
        // Constructs an EntityContainerAssociationSetEnd
        // </summary>
        // <param name="parentElement"> Reference to the schema element. </param>
        public ReferentialConstraintRoleElement(ReferentialConstraint parentElement)
            : base(parentElement)
        {
        }

        public IList<PropertyRefElement> RoleProperties
        {
            get
            {
                if (_roleProperties == null)
                {
                    _roleProperties = new List<PropertyRefElement>();
                }
                return _roleProperties;
            }
        }

        public IRelationshipEnd End
        {
            get { return _end; }
        }

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.PropertyRef))
            {
                HandlePropertyRefElement(reader);
                return true;
            }

            return false;
        }

        protected override bool HandleAttribute(XmlReader reader)
        {
            if (CanHandleAttribute(reader, XmlConstants.Role))
            {
                HandleRoleAttribute(reader);
                return true;
            }

            return false;
        }

        private void HandlePropertyRefElement(XmlReader reader)
        {
            var property = new PropertyRefElement(ParentElement);
            property.Parse(reader);
            RoleProperties.Add(property);
        }

        private void HandleRoleAttribute(XmlReader reader)
        {
            string roleName;
            Utils.GetString(Schema, reader, out roleName);
            Name = roleName;
        }

        // <summary>
        // Used during the resolve phase to resolve the type name to the object that represents that type
        // </summary>
        internal override void ResolveTopLevelNames()
        {
            Debug.Assert(!String.IsNullOrEmpty(Name), "RoleName should never be empty");
            var relationship = (IRelationship)ParentElement.ParentElement;

            if (!relationship.TryGetEnd(Name, out _end))
            {
                AddError(
                    ErrorCode.InvalidRoleInRelationshipConstraint,
                    EdmSchemaErrorSeverity.Error,
                    Strings.InvalidEndRoleInRelationshipConstraint(Name, relationship.Name));

                return;
            }

            // we are gauranteed that the _end has gone through ResolveNames, but 
            // we are not gauranteed that it was successful
            if (_end.Type == null)
            {
                // an error has already been added for this
                return;
            }
        }

        internal override void Validate()
        {
            base.Validate();
            // we can't resolve these names until validate because they will reference properties and types
            // that may not be resolved when this objects ResolveNames gets called
            Debug.Assert(
                _roleProperties != null,
                "xsd should have verified that there should be atleast one property ref element in referential role element");
            foreach (var property in _roleProperties)
            {
                if (!property.ResolveNames(_end.Type))
                {
                    AddError(
                        ErrorCode.InvalidPropertyInRelationshipConstraint,
                        EdmSchemaErrorSeverity.Error,
                        Strings.InvalidPropertyInRelationshipConstraint(
                            property.Name,
                            Name));
                }
            }
        }
    }
}
