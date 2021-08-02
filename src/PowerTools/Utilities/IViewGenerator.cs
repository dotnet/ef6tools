// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.DbContextPackage.Utilities
{
    interface IViewGenerator
    {
        string ContextTypeName { get; set; }
        string MappingHashValue { get; set; }
        dynamic Views { get; set; }

        string TransformText();
    }

    partial class CSharpViewGenerator : IViewGenerator
    {
    }

    partial class VBViewGenerator : IViewGenerator
    {
    }
}
