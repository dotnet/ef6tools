// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.DataClasses
{
    /// <summary>
    /// Attribute identifying the Edm base class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EdmEntityTypeAttribute : EdmTypeAttribute
    {
    }
}
