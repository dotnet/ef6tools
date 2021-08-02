// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    using System;

    // Event handler that knows about CommandProcessorContext
    internal delegate void CommandEventHandler(object sender, CommandEventArgs args);

    internal class CommandEventArgs : EventArgs
    {
        internal CommandEventArgs(CommandProcessorContext cpc)
        {
            CommandProcessorContext = cpc;
        }

        internal CommandProcessorContext CommandProcessorContext { get; set; }
    }
}
