// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace UnitTests.TestHelpers
{
    using System;
    using System.Diagnostics;

    internal sealed class CleanupAction : IDisposable
    {
        private readonly Action _disposeAction;

        public CleanupAction(Action disposeAction)
        {
            Debug.Assert(disposeAction != null, "disposeAction is null.");

            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction();
        }
    }
}

