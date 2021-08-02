// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model
{
    using System;

    internal class EntityQualifiedPropertyName : Tuple<string, string>
    {
        internal string EntityName
        {
            get { return Item1; }
        }

        internal string PropertyName
        {
            get { return Item2; }
        }

        internal bool IsInComplexProperty { get; private set; }

        internal EntityQualifiedPropertyName(string entityName, string propertyName, bool isInComplexProperty)
            : base(entityName, propertyName)
        {
            IsInComplexProperty = isInComplexProperty;
        }
    }
}
