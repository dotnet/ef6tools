// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    /// <summary>
    ///     Interface that allows a command to report whether or not it has caused the artifact to become dirty.
    /// </summary>
    internal interface IDocDataDirtyCommand
    {
        bool IsDocDataDirty { get; }
    }
}
