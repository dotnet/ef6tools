// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm.Provider;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Represents an enumeration type.
    /// </summary>
    public class EnumType : SimpleType
    {
        // <summary>
        // A collection of enumeration members for this enumeration type
        // </summary>
        private readonly ReadOnlyMetadataCollection<EnumMember> _members =
            new ReadOnlyMetadataCollection<EnumMember>(new MetadataCollection<EnumMember>());

        // <summary>
        // Underlying type of this enumeration type.
        // </summary>
        private PrimitiveType _underlyingType;

        private bool _isFlags;

        // <summary>
        // Initializes a new instance of the EnumType class. This default constructor is used for bootstraping
        // </summary>
        internal EnumType()
        {
            _underlyingType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);
            _isFlags = false;
        }

        // <summary>
        // Initializes a new instance of the EnumType class by using the specified <paramref name="name" />,
        // <paramref name="namespaceName" /> and <paramref name="isFlags" />.
        // </summary>
        // <param name="name"> The name of this enum type. </param>
        // <param name="namespaceName"> The namespace this enum type belongs to. </param>
        // <param name="underlyingType"> Underlying type of this enumeration type. </param>
        // <param name="isFlags"> Indicates whether the enum type is defined as flags (i.e. can be treated as a bit field). </param>
        // <param name="dataSpace"> DataSpace this enum type lives in. Can be either CSpace or OSpace </param>
        // <exception cref="System.ArgumentNullException">Thrown if name or namespace arguments are null</exception>
        // <remarks>
        // Note that enums live only in CSpace.
        // </remarks>
        internal EnumType(string name, string namespaceName, PrimitiveType underlyingType, bool isFlags, DataSpace dataSpace)
            : base(name, namespaceName, dataSpace)
        {
            DebugCheck.NotNull(underlyingType);
            Debug.Assert(Helper.IsSupportedEnumUnderlyingType(underlyingType.PrimitiveTypeKind), "Unsupported underlying type for enum.");
            Debug.Assert(dataSpace == DataSpace.CSpace || dataSpace == DataSpace.OSpace, "Enums can be only defined in CSpace or OSpace.");

            _isFlags = isFlags;
            _underlyingType = underlyingType;
        }

        // <summary>
        // Initializes a new instance of the EnumType class from CLR enumeration type.
        // </summary>
        // <param name="clrType"> CLR enumeration type to create EnumType from. </param>
        // <remarks>
        // Note that this method expects that the <paramref name="clrType" /> is a valid CLR enum type
        // whose underlying type is a valid EDM primitive type.
        // Ideally this constructor should be protected and internal (Family and Assembly modifier) but
        // C# does not support this. In order to not expose this constructor to everyone internal is the
        // only option.
        // </remarks>
        internal EnumType(Type clrType)
            :
                base(clrType.Name, clrType.NestingNamespace() ?? string.Empty, DataSpace.OSpace)
        {
            DebugCheck.NotNull(clrType);
            Debug.Assert(clrType.IsEnum(), "enum type expected");

            ClrProviderManifest.Instance.TryGetPrimitiveType(clrType.GetEnumUnderlyingType(), out _underlyingType);

            Debug.Assert(_underlyingType != null, "only primitive types expected here.");
            Debug.Assert(
                Helper.IsSupportedEnumUnderlyingType(_underlyingType.PrimitiveTypeKind),
                "unsupported CLR types should have been filtered out by .TryGetPrimitiveType() method.");

            _isFlags = clrType.GetCustomAttributes<FlagsAttribute>(inherit: false).Any();

            foreach (var name in Enum.GetNames(clrType))
            {
                AddMember(
                    new EnumMember(
                        name,
                        Convert.ChangeType(Enum.Parse(clrType, name), clrType.GetEnumUnderlyingType(), CultureInfo.InvariantCulture)));
            }
        }

        /// <summary> Returns the kind of the type </summary>
        public override BuiltInTypeKind BuiltInTypeKind
        {
            get { return BuiltInTypeKind.EnumType; }
        }

        /// <summary> Gets a collection of enumeration members for this enumeration type. </summary>
        [MetadataProperty(BuiltInTypeKind.EnumMember, true)]
        public ReadOnlyMetadataCollection<EnumMember> Members
        {
            get { return _members; }
        }

        /// <summary> Gets a value indicating whether the enum type is defined as flags (i.e. can be treated as a bit field) </summary>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        [MetadataProperty(PrimitiveTypeKind.Boolean, false)]
        public bool IsFlags
        {
            get { return _isFlags; }
            internal set
            {
                Util.ThrowIfReadOnly(this);

                _isFlags = value;
            }
        }

        /// <summary> Gets the underlying type for this enumeration type. </summary>
        [MetadataProperty(BuiltInTypeKind.PrimitiveType, false)]
        public PrimitiveType UnderlyingType
        {
            get { return _underlyingType; }
            internal set
            {
                Util.ThrowIfReadOnly(this);

                _underlyingType = value;
            }
        }

        // <summary>
        // Sets this item to be readonly, once this is set, the item will never be writable again.
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
        // Adds the specified member to the member collection
        // </summary>
        // <param name="enumMember"> Enumeration member to add to the member collection. </param>
        internal void AddMember(EnumMember enumMember)
        {
            DebugCheck.NotNull(enumMember);
            Debug.Assert(
                Helper.IsEnumMemberValueInRange(
                    UnderlyingType.PrimitiveTypeKind, Convert.ToInt64(enumMember.Value, CultureInfo.InvariantCulture)));
            Debug.Assert(enumMember.Value.GetType() == UnderlyingType.ClrEquivalentType);

            Members.Source.Add(enumMember);
        }

        /// <summary>
        /// Creates a read-only EnumType instance.
        /// </summary>
        /// <param name="name">The name of the enumeration type.</param>
        /// <param name="namespaceName">The namespace of the enumeration type.</param>
        /// <param name="underlyingType">The underlying type of the enumeration type.</param>
        /// <param name="isFlags">Indicates whether the enumeration type can be treated as a bit field; that is, a set of flags.</param>
        /// <param name="members">The members of the enumeration type.</param>
        /// <param name="metadataProperties">Metadata properties to be associated with the enumeration type.</param>
        /// <returns>The newly created EnumType instance.</returns>
        /// <exception cref="System.ArgumentNullException">underlyingType is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// name is null or empty.
        /// -or-
        /// namespaceName is null or empty.
        /// -or-
        /// underlyingType is not a supported underlying type.
        /// -or-
        /// The specified members do not have unique names.
        /// -or-
        /// The value of a specified member is not in the range of the underlying type.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        public static EnumType Create(
            string name,
            string namespaceName,
            PrimitiveType underlyingType,
            bool isFlags,
            IEnumerable<EnumMember> members,
            IEnumerable<MetadataProperty> metadataProperties)
        {
            Check.NotEmpty(name, "name");
            Check.NotEmpty(namespaceName, "namespaceName");
            Check.NotNull(underlyingType, "underlyingType");

            if (!Helper.IsSupportedEnumUnderlyingType(underlyingType.PrimitiveTypeKind))
            {
                throw new ArgumentException(Strings.InvalidEnumUnderlyingType, "underlyingType");
            }

            var instance = new EnumType(name, namespaceName, underlyingType, isFlags, DataSpace.CSpace);

            if (members != null)
            {
                foreach (var member in members)
                {
                    if (!Helper.IsEnumMemberValueInRange(
                        underlyingType.PrimitiveTypeKind, Convert.ToInt64(member.Value, CultureInfo.InvariantCulture)))
                    {
                        throw new ArgumentException(
                            Strings.EnumMemberValueOutOfItsUnderylingTypeRange(
                                member.Value, member.Name, underlyingType.Name),
                            "members");
                    }

                    instance.AddMember(member);
                }
            }

            if (metadataProperties != null)
            {
                instance.AddMetadataProperties(metadataProperties.ToList());
            }

            instance.SetReadOnly();

            return instance;
        }
    }
}
