// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.EntitySql
{
    // <summary>
    // Abstract class representing the result of an eSQL expression classification.
    // </summary>
    internal abstract class ExpressionResolution
    {
        protected ExpressionResolution(ExpressionResolutionClass @class)
        {
            ExpressionClass = @class;
        }

        internal readonly ExpressionResolutionClass ExpressionClass;
        internal abstract string ExpressionClassName { get; }
    }
}
