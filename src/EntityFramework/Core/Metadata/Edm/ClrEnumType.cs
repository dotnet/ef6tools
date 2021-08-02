// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    // <summary>
    // Represents an enumeration type that has a reference to the backing CLR type.
    // </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
    internal sealed class ClrEnumType : EnumType
    {
        private readonly Type _type;

        private readonly string _cspaceTypeName;

        // <summary>
        // Initializes a new instance of ClrEnumType class with properties from the CLR type.
        // </summary>
        // <param name="clrType"> The CLR type to construct from. </param>
        // <param name="cspaceNamespaceName"> CSpace namespace name. </param>
        // <param name="cspaceTypeName"> CSpace type name. </param>
        internal ClrEnumType(Type clrType, string cspaceNamespaceName, string cspaceTypeName)
            : base(clrType)
        {
            DebugCheck.NotNull(clrType);
            DebugCheck.NotEmpty(cspaceNamespaceName);
            DebugCheck.NotEmpty(cspaceTypeName);
            Debug.Assert(clrType.IsEnum(), "enum type expected");

            _type = clrType;
            _cspaceTypeName = cspaceNamespaceName + "." + cspaceTypeName;
        }

        // <summary>
        // Gets the clr type backing this enum type.
        // </summary>
        internal override Type ClrType
        {
            get { return _type; }
        }

        // <summary>
        // Get the full CSpaceTypeName for this enum type.
        // </summary>
        internal string CSpaceTypeName
        {
            get { return _cspaceTypeName; }
        }
    }
}
