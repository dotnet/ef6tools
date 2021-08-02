// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;

    /// <summary>
    /// Represents the Structural Type
    /// </summary>
    public abstract class StructuralType : EdmType
    {
        private readonly MemberCollection _members;
        private readonly ReadOnlyMetadataCollection<EdmMember> _readOnlyMembers;

        // <summary>
        // Internal parameterless constructor for bootstrapping edmtypes
        // </summary>
        internal StructuralType()
        {
            _members = new MemberCollection(this);
            _readOnlyMembers = _members.AsReadOnlyMetadataCollection();
        }

        // <summary>
        // Initializes a new instance of Structural Type with the given members
        // </summary>
        // <param name="name"> name of the structural type </param>
        // <param name="namespaceName"> namespace of the structural type </param>
        // <param name="dataSpace"> dataSpace in which this edmtype belongs to </param>
        // <exception cref="System.ArgumentNullException">Thrown if either name, namespace or version arguments are null</exception>
        internal StructuralType(string name, string namespaceName, DataSpace dataSpace)
            : base(name, namespaceName, dataSpace)
        {
            _members = new MemberCollection(this);
            _readOnlyMembers = _members.AsReadOnlyMetadataCollection();
        }

        /// <summary>Gets the list of members on this type.</summary>
        /// <returns>
        /// A collection of type <see cref="T:System.Data.Entity.Core.Metadata.Edm.ReadOnlyMetadataCollection`1" /> that contains a set of members on this type.
        /// </returns>
        [MetadataProperty(BuiltInTypeKind.EdmMember, true)]
        public ReadOnlyMetadataCollection<EdmMember> Members
        {
            get { return _readOnlyMembers; }
        }

        // <summary>
        // Get the declared only members of a particular type
        // </summary>
        internal ReadOnlyMetadataCollection<T> GetDeclaredOnlyMembers<T>()
            where T : EdmMember
        {
            return _members.GetDeclaredOnlyMembers<T>();
        }

        // <summary>
        // Validates the types and sets the readOnly property to true. Once the type is set to readOnly,
        // it can never be changed.
        // </summary>
        internal override void SetReadOnly()
        {
            if (!IsReadOnly)
            {
                base.SetReadOnly();
                Members.Source.SetReadOnly();
            }
        }

        // <summary>
        // Validates a EdmMember object to determine if it can be added to this type's
        // Members collection. If this method returns without throwing, it is assumed
        // the member is valid.
        // </summary>
        // <param name="member"> The member to validate </param>
        internal abstract void ValidateMemberForAdd(EdmMember member);

        /// <summary>
        /// Adds a member to this type
        /// </summary>
        /// <param name="member"> The member to add </param>
        public void AddMember(EdmMember member)
        {
            AddMember(member, false);
        }

        // <summary>
        // Adds a member to this type.
        // </summary>
        // <param name="member">The member to add.</param>
        // <param name="forceAdd">
        // Indicates whether the addition is forced, regardless of
        // whether read-only is set.
        // </param>
        // <remarks>
        // Adding a NavigationProperty to an EntityType introduces a circular dependency between
        // EntityType and AssociationEndMember, which is worked around by calling this method.
        // This is the case of OneToOneMappingBuilder, in the designer. Must not be used in other context.
        // </remarks>
        internal void AddMember(EdmMember member, bool forceAdd)
        {
            Check.NotNull(member, "member");

            if (!forceAdd)
            {
                Util.ThrowIfReadOnly(this);
            }

            if (DataSpace != member.TypeUsage.EdmType.DataSpace
                && BuiltInTypeKind != BuiltInTypeKind.RowType)
            {
                throw new ArgumentException(
                    Strings.AttemptToAddEdmMemberFromWrongDataSpace(
                        member.Name,
                        this.Name,
                        member.TypeUsage.EdmType.DataSpace,
                        this.DataSpace),
                    "member");
            }

            // Since we set the DataSpace of the RowType to be -1 in the constructor, we need to initialize it
            // as and when we add members to it
            if (BuiltInTypeKind.RowType == BuiltInTypeKind)
            {
                // Do this only when you are adding the first member
                if (_members.Count == 0)
                {
                    DataSpace = member.TypeUsage.EdmType.DataSpace;
                }
                // We need to build types that span across more than one space. For such row types, we set the 
                // DataSpace to -1
                else if (DataSpace != (DataSpace)(-1)
                         && member.TypeUsage.EdmType.DataSpace != DataSpace)
                {
                    DataSpace = (DataSpace)(-1);
                }
            }

            if (_members.IsReadOnly && forceAdd)
            {
                _members.ResetReadOnly();
                _members.Add(member);
                _members.SetReadOnly();
            }
            else
            {
                _members.Add(member);
            }
        }

        /// <summary>Removes a member from this type.</summary>
        /// <param name="member">The member to remove.</param>
        public virtual void RemoveMember(EdmMember member)
        {
            Check.NotNull(member, "member");
            Util.ThrowIfReadOnly(this);

            _members.Remove(member);
        }

        internal virtual bool HasMember(EdmMember member)
        {
            DebugCheck.NotNull(member);

            return _members.Contains(member);
        }

        internal virtual void NotifyItemIdentityChanged(EdmMember item, string initialIdentity)
        {
            _members.HandleIdentityChange(item, initialIdentity);
        }
    }
}
