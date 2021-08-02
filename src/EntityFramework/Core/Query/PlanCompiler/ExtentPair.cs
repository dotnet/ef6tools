// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.PlanCompiler
{
    using System.Data.Entity.Core.Metadata.Edm;

    // <summary>
    // A simple class that represents a pair of extents
    // </summary>
    internal class ExtentPair
    {
        #region public surface

        // <summary>
        // Return the left component of the pair
        // </summary>
        internal EntitySetBase Left
        {
            get { return m_left; }
        }

        // <summary>
        // Return the right component of the pair
        // </summary>
        internal EntitySetBase Right
        {
            get { return m_right; }
        }

        // <summary>
        // Equals
        // </summary>
        public override bool Equals(object obj)
        {
            var other = obj as ExtentPair;
            return (other != null) && other.Left.Equals(Left) && other.Right.Equals(Right);
        }

        // <summary>
        // Hashcode
        // </summary>
        public override int GetHashCode()
        {
            return (Left.GetHashCode() << 4) ^ Right.GetHashCode();
        }

        #endregion

        #region constructors

        internal ExtentPair(EntitySetBase left, EntitySetBase right)
        {
            m_left = left;
            m_right = right;
        }

        #endregion

        #region private state

        private readonly EntitySetBase m_left;
        private readonly EntitySetBase m_right;

        #endregion
    }
}
