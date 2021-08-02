// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    /// <summary>
    /// Represents the multiplicity information about the end of a relationship type
    /// </summary>
    public enum RelationshipMultiplicity
    {
        /// <summary>
        /// Lower Bound is Zero and Upper Bound is One
        /// </summary>
        ZeroOrOne,

        /// <summary>
        /// Both lower bound and upper bound is one
        /// </summary>
        One,

        /// <summary>
        /// Lower bound is zero and upper bound is null
        /// </summary>
        Many
    }
}
