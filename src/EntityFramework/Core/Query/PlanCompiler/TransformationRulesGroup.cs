// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.PlanCompiler
{
    // <summary>
    // Available groups of rules, not necessarily mutually exclusive
    // </summary>
    internal enum TransformationRulesGroup
    {
        All,
        Project,
        PostJoinElimination,
        NullSemantics
    }
}
