// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    using System.Xml.Linq;

    internal abstract class CommandTextBase : EFElement
    {
        internal CommandTextBase(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        internal string Command
        {
            get { return XElement.Value; }
        }
    }
}
