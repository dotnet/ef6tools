// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    using System;

    internal class DefaultableEnumTypeMemberValue : DefaultableValue<string>
    {
        internal static readonly string ValueAttribute = "Value";

        internal DefaultableEnumTypeMemberValue(EFElement parent)
            : base(parent, ValueAttribute)
        {
        }

        internal override string AttributeName
        {
            get { return ValueAttribute; }
        }

        public override string DefaultValue
        {
            get { return String.Empty; }
        }
    }
}
