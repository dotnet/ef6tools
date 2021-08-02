// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Extensibility
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.Data.Entity.Design.Model;
    using Microsoft.Data.Entity.Design.UI;

    internal class LayerSelection : Selection
    {
        public LayerSelection()
        {
        }

        internal LayerSelection(IEnumerable<EFObject> selectedObjects)
            : base(selectedObjects)
        {
        }

        internal LayerSelection(IEnumerable<EFObject> selectedObjects, Predicate<EFObject> match)
            : base(selectedObjects, match)
        {
        }

        internal LayerSelection(IEnumerable selectedObjects)
            : base(selectedObjects)
        {
        }

        internal LayerSelection(IEnumerable selectedObjects, Predicate<EFObject> match)
            : base(selectedObjects, match)
        {
        }

        internal LayerSelection(params EFObject[] selectedObjects)
            : base(selectedObjects)
        {
        }

        internal override Type ItemType
        {
            get { return typeof(LayerSelection); }
        }
    }
}
