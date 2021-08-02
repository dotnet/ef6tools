// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Xml;

    // <summary>
    // Represents an End element in a relationship
    // </summary>
    internal sealed class RelationshipEnd : SchemaElement, IRelationshipEnd
    {
        private string _unresolvedType;
        private RelationshipMultiplicity? _multiplicity;
        private List<OnOperation> _operations;

        // <summary>
        // construct a Relationship End
        // </summary>
        public RelationshipEnd(Relationship relationship)
            : base(relationship)
        {
        }

        // <summary>
        // Type of the End
        // </summary>
        public SchemaEntityType Type { get; private set; }

        // <summary>
        // Multiplicity of the End
        // </summary>
        public RelationshipMultiplicity? Multiplicity
        {
            get { return _multiplicity; }
            set { _multiplicity = value; }
        }

        // <summary>
        // The On&lt;Operation&gt;s defined for the End
        // </summary>
        public ICollection<OnOperation> Operations
        {
            get
            {
                if (_operations == null)
                {
                    _operations = new List<OnOperation>();
                }
                return _operations;
            }
        }

        // <summary>
        // do whole element resolution
        // </summary>
        internal override void ResolveTopLevelNames()
        {
            base.ResolveTopLevelNames();

            if (Type == null
                && _unresolvedType != null)
            {
                SchemaType element;
                if (!Schema.ResolveTypeName(this, _unresolvedType, out element))
                {
                    return;
                }

                Type = element as SchemaEntityType;
                if (Type == null)
                {
                    AddError(
                        ErrorCode.InvalidRelationshipEndType, EdmSchemaErrorSeverity.Error,
                        Strings.InvalidRelationshipEndType(ParentElement.Name, element.FQName));
                }
            }
        }

        internal override void Validate()
        {
            base.Validate();

            // Check if the end has multiplicity as many, it cannot have any operation behaviour
            if (Multiplicity == RelationshipMultiplicity.Many
                && Operations.Count != 0)
            {
                AddError(
                    ErrorCode.EndWithManyMultiplicityCannotHaveOperationsSpecified,
                    EdmSchemaErrorSeverity.Error,
                    Strings.EndWithManyMultiplicityCannotHaveOperationsSpecified(Name, ParentElement.FQName));
            }

            // if there is no RefConstraint in Association and multiplicity is null
            if (ParentElement.Constraints.Count == 0
                && Multiplicity == null)
            {
                AddError(
                    ErrorCode.EndWithoutMultiplicity,
                    EdmSchemaErrorSeverity.Error,
                    Strings.EndWithoutMultiplicity(Name, ParentElement.FQName));
            }
        }

        // <summary>
        // Do simple validation across attributes
        // </summary>
        protected override void HandleAttributesComplete()
        {
            // set up the default name in before validating anythig that might want to display it in an error message;
            if (Name == null
                && _unresolvedType != null)
            {
                Name = Utils.ExtractTypeName(_unresolvedType);
            }

            base.HandleAttributesComplete();
        }

        protected override bool ProhibitAttribute(string namespaceUri, string localName)
        {
            if (base.ProhibitAttribute(namespaceUri, localName))
            {
                return true;
            }

            if (namespaceUri == null
                && localName == XmlConstants.Name)
            {
                return false;
            }
            return false;
        }

        protected override bool HandleAttribute(XmlReader reader)
        {
            if (base.HandleAttribute(reader))
            {
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.Multiplicity))
            {
                HandleMultiplicityAttribute(reader);
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.Role))
            {
                HandleNameAttribute(reader);
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.TypeElement))
            {
                HandleTypeAttribute(reader);
                return true;
            }

            return false;
        }

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.OnDelete))
            {
                HandleOnDeleteElement(reader);
                return true;
            }
            return false;
        }

        // <summary>
        // Handle the Type attribute
        // </summary>
        // <param name="reader"> reader positioned at Type attribute </param>
        private void HandleTypeAttribute(XmlReader reader)
        {
            DebugCheck.NotNull(reader);

            string type;
            if (!Utils.GetDottedName(Schema, reader, out type))
            {
                return;
            }

            _unresolvedType = type;
        }

        // <summary>
        // Handle the Multiplicity attribute
        // </summary>
        // <param name="reader"> reader positioned at Type attribute </param>
        private void HandleMultiplicityAttribute(XmlReader reader)
        {
            DebugCheck.NotNull(reader);
            RelationshipMultiplicity multiplicity;
            if (!RelationshipMultiplicityConverter.TryParseMultiplicity(reader.Value, out multiplicity))
            {
                AddError(
                    ErrorCode.InvalidMultiplicity, EdmSchemaErrorSeverity.Error, reader,
                    Strings.InvalidRelationshipEndMultiplicity(ParentElement.Name, reader.Value));
            }
            _multiplicity = multiplicity;
        }

        // <summary>
        // Handle an OnDelete element
        // </summary>
        // <param name="reader"> reader positioned at the element </param>
        private void HandleOnDeleteElement(XmlReader reader)
        {
            HandleOnOperationElement(reader, Operation.Delete);
        }

        // <summary>
        // Handle an On&lt;Operation&gt; element
        // </summary>
        // <param name="reader"> reader positioned at the element </param>
        // <param name="operation"> the kind of operation being handled </param>
        private void HandleOnOperationElement(XmlReader reader, Operation operation)
        {
            DebugCheck.NotNull(reader);

            foreach (var other in Operations)
            {
                if (other.Operation == operation)
                {
                    AddError(ErrorCode.InvalidOperation, EdmSchemaErrorSeverity.Error, reader, Strings.DuplicationOperation(reader.Name));
                }
            }

            var onOperation = new OnOperation(this, operation);
            onOperation.Parse(reader);
            _operations.Add(onOperation);
        }

        // <summary>
        // The parent element as an IRelationship
        // </summary>
        internal new IRelationship ParentElement
        {
            get { return (IRelationship)(base.ParentElement); }
        }
    }
}
