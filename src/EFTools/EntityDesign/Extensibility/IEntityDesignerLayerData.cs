// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{
    using System.ComponentModel;

    /// <summary>
    ///     Parent interface used by the LayerManager to distinguish different layers.
    /// </summary>
    public interface IEntityDesignerLayerData
    {
        /// <summary>
        ///     The name of this extensibility layer.
        /// </summary>
        [DefaultValue(null)]
        string LayerName { get; }
    }
}
