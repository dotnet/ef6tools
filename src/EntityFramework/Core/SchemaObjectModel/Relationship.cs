// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Xml;

    // <summary>
    // Represents an Association element
    // </summary>
    internal sealed class Relationship : SchemaType, IRelationship
    {
        private RelationshipEndCollection _ends;
        private List<ReferentialConstraint> _constraints;
        private bool _isForeignKey;

        // <summary>
        // Construct a Relationship object
        // </summary>
        // <param name="parent"> the parent </param>
        // <param name="kind"> the kind of relationship </param>
        public Relationship(Schema parent, RelationshipKind kind)
            : base(parent)
        {
            RelationshipKind = kind;

            if (Schema.DataModel
                == SchemaDataModelOption.EntityDataModel)
            {
                _isForeignKey = false;
                OtherContent.Add(Schema.SchemaSource);
            }
            else if (Schema.DataModel
                     == SchemaDataModelOption.ProviderDataModel)
            {
                _isForeignKey = true;
            }
        }

        // <summary>
        // List of Ends defined for this Association
        // </summary>
        public IList<IRelationshipEnd> Ends
        {
            get
            {
                if (_ends == null)
                {
                    _ends = new RelationshipEndCollection();
                }
                return _ends;
            }
        }

        // <summary>
        // Returns the list of constraints on this relation
        // </summary>
        public IList<ReferentialConstraint> Constraints
        {
            get
            {
                if (_constraints == null)
                {
                    _constraints = new List<ReferentialConstraint>();
                }
                return _constraints;
            }
        }

        public bool TryGetEnd(string roleName, out IRelationshipEnd end)
        {
            return _ends.TryGetEnd(roleName, out end);
        }

        // <summary>
        // Is this an Association
        // </summary>
        public RelationshipKind RelationshipKind { get; private set; }

        // <summary>
        // Is this a foreign key (aka foreign key) relationship?
        // </summary>
        public bool IsForeignKey
        {
            get { return _isForeignKey; }
        }

        // <summary>
        // do whole element validation
        // </summary>
        internal override void Validate()
        {
            base.Validate();

            var foundOperations = false;
            foreach (RelationshipEnd end in Ends)
            {
                end.Validate();
                if (RelationshipKind == RelationshipKind.Association)
                {
                    if (end.Operations.Count > 0)
                    {
                        if (foundOperations)
                        {
                            end.AddError(
                                ErrorCode.InvalidOperation, EdmSchemaErrorSeverity.Error, Strings.InvalidOperationMultipleEndsInAssociation);
                        }
                        foundOperations = true;
                    }
                }
            }

            if (Constraints.Count == 0)
            {
                if (Schema.DataModel
                    == SchemaDataModelOption.ProviderDataModel)
                {
                    AddError(
                        ErrorCode.MissingConstraintOnRelationshipType,
                        EdmSchemaErrorSeverity.Error,
                        Strings.MissingConstraintOnRelationshipType(FQName));
                }
            }
            else
            {
                foreach (var constraint in Constraints)
                {
                    constraint.Validate();
                }
            }
        }

        // <summary>
        // do whole element resolution
        // </summary>
        internal override void ResolveTopLevelNames()
        {
            base.ResolveTopLevelNames();

            foreach (RelationshipEnd end in Ends)
            {
                end.ResolveTopLevelNames();
            }

            foreach (var referentialConstraint in Constraints)
            {
                referentialConstraint.ResolveTopLevelNames();
            }
        }

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.End))
            {
                HandleEndElement(reader);
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.ReferentialConstraint))
            {
                HandleConstraintElement(reader);
                return true;
            }
            return false;
        }

        // <summary>
        // handle the End child element
        // </summary>
        // <param name="reader"> XmlReader positioned at the end element </param>
        private void HandleEndElement(XmlReader reader)
        {
            DebugCheck.NotNull(reader);
            var end = new RelationshipEnd(this);
            end.Parse(reader);

            if (Ends.Count == 2)
            {
                AddError(ErrorCode.InvalidAssociation, EdmSchemaErrorSeverity.Error, Strings.TooManyAssociationEnds(FQName));
                return;
            }

            Ends.Add(end);
        }

        // <summary>
        // handle the constraint element
        // </summary>
        // <param name="reader"> XmlReader positioned at the constraint element </param>
        private void HandleConstraintElement(XmlReader reader)
        {
            DebugCheck.NotNull(reader);

            var constraint = new ReferentialConstraint(this);
            constraint.Parse(reader);
            Constraints.Add(constraint);

            if (Schema.DataModel == SchemaDataModelOption.EntityDataModel
                && Schema.SchemaVersion >= XmlConstants.EdmVersionForV2)
            {
                // in V2, referential constraint implies foreign key
                _isForeignKey = true;
            }
        }
    }
}
