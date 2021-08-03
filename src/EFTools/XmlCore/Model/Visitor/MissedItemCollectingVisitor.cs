// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Visitor
{
    using System.Collections.Generic;
    using Microsoft.Data.Tools.XmlDesignerBase.Base.Util;

    internal abstract class MissedItemCollectingVisitor : Visitor
    {
        protected int _missedCount = -1;

        // use a hash-set here because containment checks on lists are too expensive. 
        protected HashSet<EFElement> _missed = new HashSet<EFElement>();

        internal int MissedCount
        {
            get { return _missedCount; }
        }

        internal ICollection<EFElement> Missed
        {
            get { return new ReadOnlyCollection<EFElement>(_missed); }
        }

        internal void ResetMissedCount()
        {
            _missedCount = 0;
        }
    }
}
