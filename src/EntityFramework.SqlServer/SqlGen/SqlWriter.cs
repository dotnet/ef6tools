// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.SqlGen
{
    using System.Data.Entity.Migrations.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;

    // <summary>
    // This extends IndentedTextWriter/StringWriter primarily to add the ability to add an indent
    // to each line that is written out.
    // </summary>
    internal class SqlWriter : IndentedTextWriter
    {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Transferring ownership")]
        public SqlWriter(StringBuilder b)
            : base(new StringWriter(b, Culture))
        // Culture must match what is used by underlying IndentedTextWriter
        {
        }
    }
}
