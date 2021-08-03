// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.Utilities
{
    internal static class FuncExtensions
    {
        internal static TResult NullIfNotImplemented<TResult>(this Func<TResult> func)
        {
            try
            {
                return func();
            }
            catch (NotImplementedException)
            {
                return default(TResult);
            }
        }
    }
}
