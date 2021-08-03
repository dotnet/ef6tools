// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.SqlGen
{
    // <summary>
    // Used for wrapping a boolean value as an object.
    // </summary>
    internal class BoolWrapper
    {
        internal bool Value { get; set; }

        internal BoolWrapper()
        {
            Value = false;
        }
    }
}
