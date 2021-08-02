﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    using System;

    internal class DelegateCommand : Command
    {
        private readonly Action _delegateCommandCallback;

        internal DelegateCommand(Action delegateCommandCallback)
        {
            _delegateCommandCallback = delegateCommandCallback;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            _delegateCommandCallback();
        }
    }
}
