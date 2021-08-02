// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    /// <summary>
    /// The enumeration defining the mode of a parameter
    /// </summary>
    public enum ParameterMode
    {
        /// <summary>
        /// In parameter
        /// </summary>
        In = 0,

        /// <summary>
        /// Out parameter
        /// </summary>
        Out,

        /// <summary>
        /// Both in and out parameter
        /// </summary>
        InOut,

        /// <summary>
        /// Return Parameter
        /// </summary>
        ReturnValue
    }
}
