﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Database
{
    using System.Collections.Generic;

    internal interface IDataSchemaProcedure : IRawDataSchemaProcedure
    {
        IList<IDataSchemaParameter> Parameters { get; }
        IList<IDataSchemaColumn> Columns { get; }
        IDataSchemaParameter ReturnValue { get; }
    }
}
