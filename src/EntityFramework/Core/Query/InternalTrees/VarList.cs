// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    // <summary>
    // An ordered list of Vars. Use this when you need an ordering.
    // </summary>
    [DebuggerDisplay("{{{ToString()}}}")]
    internal class VarList : List<Var>
    {
        #region constructors

        // <summary>
        // Trivial constructor
        // </summary>
        internal VarList()
        {
        }

        // <summary>
        // Not so trivial constructor
        // </summary>
        internal VarList(IEnumerable<Var> vars)
            : base(vars)
        {
        }

        #endregion

        #region public methods

        // <summary>
        // Debugging support
        // provide a string representation for debugging.
        // </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            var separator = String.Empty;

            foreach (var v in this)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", separator, v.Id);
                separator = ",";
            }
            return sb.ToString();
        }

        #endregion
    }
}
