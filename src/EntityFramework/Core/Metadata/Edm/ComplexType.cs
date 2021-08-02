// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Represents the Edm Complex Type.  This can be used to configure complex types
    /// from a conceptual-space model-based convention. Complex types are not supported in the store model.
    /// </summary>
    public class ComplexType : StructuralType
    {
        // <summary>
        // Initializes a new instance of Complex Type with the given properties
        // </summary>
        // <param name="name"> The name of the complex type </param>
        // <param name="namespaceName"> The namespace name of the type </param>
        // <param name="dataSpace"> dataSpace in which this ComplexType belongs to </param>
        // <exception cref="System.ArgumentNullException">If either name, namespace or version arguments are null</exception>
        internal ComplexType(string name, string namespaceName, DataSpace dataSpace)
            : base(name, namespaceName, dataSpace)
        {
        }

        // <summary>
        // Initializes a new instance of Complex Type - required for bootstraping code
        // </summary>
        internal ComplexType()
        {
            // No initialization of item attributes in here, it's used as a pass thru in the case for delay population
            // of item attributes
        }

        internal ComplexType(string name)
            : this(name, EdmConstants.TransientNamespace, DataSpace.CSpace)
        {
            // testing only
        }

        /// <summary>
        /// Gets the built-in type kind for this <see cref="T:System.Data.Entity.Core.Metadata.Edm.ComplexType" />.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Entity.Core.Metadata.Edm.BuiltInTypeKind" /> object that represents the built-in type kind for this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.ComplexType" />
        /// .
        /// </returns>
        public override BuiltInTypeKind BuiltInTypeKind
        {
            get { return BuiltInTypeKind.ComplexType; }
        }

        /// <summary>
        /// Gets the list of properties for this <see cref="T:System.Data.Entity.Core.Metadata.Edm.ComplexType" />.
        /// </summary>
        /// <returns>
        /// A collection of type <see cref="T:System.Data.Entity.Core.Metadata.Edm.ReadOnlyMetadataCollection`1" /> that contains the list of properties for this
        /// <see
        ///     cref="T:System.Data.Entity.Core.Metadata.Edm.ComplexType" />
        /// .
        /// </returns>
        public virtual ReadOnlyMetadataCollection<EdmProperty> Properties
        {
            get
            {
                return new FilteredReadOnlyMetadataCollection<EdmProperty, EdmMember>(
                    Members, Helper.IsEdmProperty);
            }
        }

        // <summary>
        // Validates a EdmMember object to determine if it can be added to this type's
        // Members collection. If this method returns without throwing, it is assumed
        // the member is valid.
        // </summary>
        // <param name="member"> The member to validate </param>
        // <exception cref="System.ArgumentException">Thrown if the member is not a EdmProperty</exception>
        internal override void ValidateMemberForAdd(EdmMember member)
        {
            Debug.Assert(
                Helper.IsEdmProperty(member),
                "Only members of type Property may be added to ComplexType.");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ComplexType " /> type.
        /// </summary>
        /// <param name="name">The name of the complex type.</param>
        /// <param name="namespaceName">The namespace of the complex type.</param>
        /// <param name="dataSpace">The dataspace to which the complex type belongs to.</param>
        /// <param name="members">Members of the complex type.</param>
        /// <param name="metadataProperties">Metadata properties to be associated with the instance.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if either name, namespace or members argument is null.</exception>
        /// <returns>
        /// A new instance a the <see cref="ComplexType " /> type.
        /// </returns>
        /// <notes>
        /// The newly created <see cref="ComplexType " /> will be read only.
        /// </notes>
        public static ComplexType Create(
            string name,
            string namespaceName,
            DataSpace dataSpace,
            IEnumerable<EdmMember> members,
            IEnumerable<MetadataProperty> metadataProperties)
        {
            Check.NotEmpty(name, "name");
            Check.NotEmpty(namespaceName, "namespaceName");
            Check.NotNull(members, "members");

            var complexType = new ComplexType(name, namespaceName, dataSpace);

            foreach (var member in members)
            {
                complexType.AddMember(member);
            }

            if (metadataProperties != null)
            {
                complexType.AddMetadataProperties(metadataProperties.ToList());
            }

            complexType.SetReadOnly();
            return complexType;
        }
    }

    [SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
    internal sealed class ClrComplexType : ComplexType
    {
        private readonly Type _type;

        // <summary>
        // cached dynamic method to construct a CLR instance
        // </summary>
        private Func<object> _constructor;

        private readonly string _cspaceTypeName;

        // <summary>
        // Initializes a new instance of Complex Type with properties from the type.
        // </summary>
        // <param name="clrType"> The CLR type to construct from </param>
        internal ClrComplexType(Type clrType, string cspaceNamespaceName, string cspaceTypeName)
            : base(Check.NotNull(clrType, "clrType").Name, clrType.NestingNamespace() ?? string.Empty,
                DataSpace.OSpace)
        {
            DebugCheck.NotEmpty(cspaceNamespaceName);
            DebugCheck.NotEmpty(cspaceTypeName);

            _type = clrType;
            _cspaceTypeName = cspaceNamespaceName + "." + cspaceTypeName;
            Abstract = clrType.IsAbstract();
        }

        internal static ClrComplexType CreateReadonlyClrComplexType(Type clrType, string cspaceNamespaceName, string cspaceTypeName)
        {
            var type = new ClrComplexType(clrType, cspaceNamespaceName, cspaceTypeName);
            type.SetReadOnly();

            return type;
        }

        // <summary>
        // cached dynamic method to construct a CLR instance
        // </summary>
        internal Func<object> Constructor
        {
            get { return _constructor; }
            set
            {
                // It doesn't matter which delegate wins, but only one should be jitted
                Interlocked.CompareExchange(ref _constructor, value, null);
            }
        }

        internal override Type ClrType
        {
            get { return _type; }
        }

        internal string CSpaceTypeName
        {
            get { return _cspaceTypeName; }
        }
    }
}
