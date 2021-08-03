// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    using System.Data.Entity.Infrastructure;

    internal interface IContextGenerator
    {
        string Generate(DbModel model, string codeNamespace, string contextClassName, string connectionStringName);
    }
}