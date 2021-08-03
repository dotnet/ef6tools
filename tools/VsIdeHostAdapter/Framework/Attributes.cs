// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.TestTools.VsIdeTesting
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class VsIdePreHostExecutionMethod : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class VsIdePostHostExecutionMethod : Attribute
    {
    }
}
