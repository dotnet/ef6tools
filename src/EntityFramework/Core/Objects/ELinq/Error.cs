// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Linq.Expressions.Internal
{
    using System.Data.Entity.Resources;

    internal static class Error
    {
        internal static Exception UnhandledExpressionType(ExpressionType expressionType)
        {
            return new NotSupportedException(Strings.ELinq_UnhandledExpressionType(expressionType));
        }

        internal static Exception UnhandledBindingType(MemberBindingType memberBindingType)
        {
            return new NotSupportedException(Strings.ELinq_UnhandledBindingType(memberBindingType));
        }
    }
}
