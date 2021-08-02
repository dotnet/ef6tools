// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{
    using System.Collections.Generic;

    internal interface IEntityDesignerCommandFactory
    {
        // <summary>
        //     Commands that will be surfaced in the Entity Designer
        // </summary>
        IList<EntityDesignerCommand> Commands { get; }
    }
}
