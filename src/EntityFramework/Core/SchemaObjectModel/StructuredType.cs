// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.SchemaObjectModel
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;

    // <summary>
    // Summary description for StructuredType.
    // </summary>
    internal abstract class StructuredType : SchemaType
    {
        #region Instance Fields

        private bool? _baseTypeResolveResult;
        private string _unresolvedBaseType;
        private bool _isAbstract;
        private SchemaElementLookUpTable<SchemaElement> _namedMembers;
        private ISchemaElementLookUpTable<StructuredProperty> _properties;

        #endregion

        #region Public Properties

        public StructuredType BaseType { get; private set; }

        public ISchemaElementLookUpTable<StructuredProperty> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new FilteredSchemaElementLookUpTable<StructuredProperty, SchemaElement>(NamedMembers);
                }
                return _properties;
            }
        }

        protected SchemaElementLookUpTable<SchemaElement> NamedMembers
        {
            get
            {
                if (_namedMembers == null)
                {
                    _namedMembers = new SchemaElementLookUpTable<SchemaElement>();
                }
                return _namedMembers;
            }
        }

        public virtual bool IsTypeHierarchyRoot
        {
            get
            {
                Debug.Assert(
                    (BaseType == null && _unresolvedBaseType == null) ||
                    (BaseType != null && _unresolvedBaseType != null),
                    "you are checking for the hierarchy root before the basetype has been set");

                // any type without a base is a base type
                return BaseType == null;
            }
        }

        public bool IsAbstract
        {
            get { return _isAbstract; }
        }

        #endregion

        #region More Public Methods

        // <summary>
        // Find a property by name in the type hierarchy
        // </summary>
        // <param name="name"> simple property name </param>
        // <returns> the StructuredProperty object if name exists, null otherwise </returns>
        public StructuredProperty FindProperty(string name)
        {
            var property = Properties.LookUpEquivalentKey(name);
            if (property != null)
            {
                return property;
            }

            if (IsTypeHierarchyRoot)
            {
                return null;
            }

            return BaseType.FindProperty(name);
        }

        // <summary>
        // Determines whether this type is of the same type as baseType,
        // or is derived from baseType.
        // </summary>
        // <returns> true if this type is of the baseType, false otherwise </returns>
        public bool IsOfType(StructuredType baseType)
        {
            var type = this;

            while (type != null
                   && type != baseType)
            {
                type = type.BaseType;
            }

            return (type == baseType);
        }

        #endregion

        #region Protected Methods

        internal override void ResolveTopLevelNames()
        {
            base.ResolveTopLevelNames();

            TryResolveBaseType();

            foreach (var member in NamedMembers)
            {
                member.ResolveTopLevelNames();
            }
        }

        internal override void Validate()
        {
            base.Validate();

            foreach (var member in NamedMembers)
            {
                if (BaseType != null)
                {
                    StructuredType definingType;
                    SchemaElement definingMember;
                    string errorMessage = null;
                    if (HowDefined.AsMember
                        == BaseType.DefinesMemberName(member.Name, out definingType, out definingMember))
                    {
                        errorMessage = Strings.DuplicateMemberName(member.Name, FQName, definingType.FQName);
                    }
                    if (errorMessage != null)
                    {
                        member.AddError(ErrorCode.AlreadyDefined, EdmSchemaErrorSeverity.Error, errorMessage);
                    }
                }

                member.Validate();
            }
        }

        protected StructuredType(Schema parentElement)
            : base(parentElement)
        {
        }

        // <summary>
        // Add a member to the type
        // </summary>
        // <param name="newMember"> the member being added </param>
        protected void AddMember(SchemaElement newMember)
        {
            DebugCheck.NotNull(newMember);

            if (string.IsNullOrEmpty(newMember.Name))
            {
                // this is an error condition that has already been reported.
                return;
            }

            if (Schema.DataModel != SchemaDataModelOption.ProviderDataModel
                && Utils.CompareNames(newMember.Name, Name) == 0)
            {
                newMember.AddError(
                    ErrorCode.BadProperty, EdmSchemaErrorSeverity.Error,
                    Strings.InvalidMemberNameMatchesTypeName(newMember.Name, FQName));
            }

            NamedMembers.Add(newMember, true, Strings.PropertyNameAlreadyDefinedDuplicate);
        }

        // <summary>
        // See if a name is a member in a type or any of its base types
        // </summary>
        // <param name="name"> name to look for </param>
        // <param name="definingType"> if defined, the type that defines it </param>
        // <param name="definingMember"> if defined, the member that defines it </param>
        // <returns> how name was defined </returns>
        private HowDefined DefinesMemberName(string name, out StructuredType definingType, out SchemaElement definingMember)
        {
            if (NamedMembers.ContainsKey(name))
            {
                definingType = this;
                definingMember = NamedMembers[name];
                return HowDefined.AsMember;
            }

            definingMember = NamedMembers.LookUpEquivalentKey(name);
            Debug.Assert(definingMember == null, "we allow the scenario that members can have same name but different cases");

            if (IsTypeHierarchyRoot)
            {
                definingType = null;
                definingMember = null;
                return HowDefined.NotDefined;
            }

            return BaseType.DefinesMemberName(name, out definingType, out definingMember);
        }

        #endregion

        #region Protected Properties

        protected string UnresolvedBaseType
        {
            get { return _unresolvedBaseType; }
            set { _unresolvedBaseType = value; }
        }

        protected override bool HandleElement(XmlReader reader)
        {
            if (base.HandleElement(reader))
            {
                return true;
            }
            else if (CanHandleElement(reader, XmlConstants.Property))
            {
                HandlePropertyElement(reader);
                return true;
            }
            return false;
        }

        protected override bool HandleAttribute(XmlReader reader)
        {
            if (base.HandleAttribute(reader))
            {
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.BaseType))
            {
                HandleBaseTypeAttribute(reader);
                return true;
            }
            else if (CanHandleAttribute(reader, XmlConstants.Abstract))
            {
                HandleAbstractAttribute(reader);
                return true;
            }

            return false;
        }

        #endregion

        #region Private Methods

        private bool TryResolveBaseType()
        {
            if (_baseTypeResolveResult.HasValue)
            {
                return _baseTypeResolveResult.Value;
            }

            if (BaseType != null)
            {
                _baseTypeResolveResult = true;
                return _baseTypeResolveResult.Value;
            }

            if (UnresolvedBaseType == null)
            {
                _baseTypeResolveResult = true;
                return _baseTypeResolveResult.Value;
            }

            SchemaType element;
            if (!Schema.ResolveTypeName(this, UnresolvedBaseType, out element))
            {
                _baseTypeResolveResult = false;
                return _baseTypeResolveResult.Value;
            }

            BaseType = element as StructuredType;
            if (BaseType == null)
            {
                AddError(
                    ErrorCode.InvalidBaseType, EdmSchemaErrorSeverity.Error,
                    Strings.InvalidBaseTypeForStructuredType(UnresolvedBaseType, FQName));
                _baseTypeResolveResult = false;
                return _baseTypeResolveResult.Value;
            }

            // verify that creating this link to the base type will not introduce a cycle;
            // if so, break the link and add an error
            if (CheckForInheritanceCycle())
            {
                BaseType = null;

                AddError(
                    ErrorCode.CycleInTypeHierarchy, EdmSchemaErrorSeverity.Error,
                    Strings.CycleInTypeHierarchy(FQName));
                _baseTypeResolveResult = false;
                return _baseTypeResolveResult.Value;
            }

            _baseTypeResolveResult = true;
            return true;
        }

        private void HandleBaseTypeAttribute(XmlReader reader)
        {
            Debug.Assert(UnresolvedBaseType == null, string.Format(CultureInfo.CurrentCulture, "{0} is already defined", reader.Name));

            string baseType;
            if (!Utils.GetDottedName(Schema, reader, out baseType))
            {
                return;
            }

            UnresolvedBaseType = baseType;
        }

        private void HandleAbstractAttribute(XmlReader reader)
        {
            HandleBoolAttribute(reader, ref _isAbstract);
        }

        private void HandlePropertyElement(XmlReader reader)
        {
            var property = new StructuredProperty(this);

            property.Parse(reader);

            AddMember(property);
        }

        // <summary>
        // Determine if a cycle exists in the type hierarchy: use two pointers to
        // walk the chain, if one catches up with the other, we have a cycle.
        // </summary>
        // <returns> true if a cycle exists in the type hierarchy, false otherwise </returns>
        private bool CheckForInheritanceCycle()
        {
            var baseType = BaseType;
            Debug.Assert(baseType != null);

            var ref1 = baseType;
            var ref2 = baseType;

            do
            {
                ref2 = ref2.BaseType;

                if (ReferenceEquals(ref1, ref2))
                {
                    return true;
                }

                if (ref1 == null)
                {
                    return false;
                }

                ref1 = ref1.BaseType;

                if (ref2 != null)
                {
                    ref2 = ref2.BaseType;
                }
            }
            while (ref2 != null);

            return false;
        }

        #endregion

        #region Private Properties

        #endregion

        private enum HowDefined
        {
            NotDefined,
            AsMember,
        }
    }
}
