// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class DeleteDefaultableValueCommand<T> : Command
    {
        private readonly DefaultableValue<T> _defaultableValue;

        internal DeleteDefaultableValueCommand(DefaultableValue<T> defaultableValue)
        {
            _defaultableValue = defaultableValue;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            _defaultableValue.Delete();
        }
    }
}
