// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
    using Microsoft.Data.Entity.Design.EntityDesigner.View;

    internal abstract class EntityTypeShapeModelChange : ViewModelChange
    {
        private readonly EntityTypeShape _entityTypeShape;

        internal override bool IsDiagramChange
        {
            get { return true; }
        }

        protected EntityTypeShapeModelChange(EntityTypeShape entityTypeShape)
        {
            _entityTypeShape = entityTypeShape;
        }

        public EntityTypeShape EntityTypeShape
        {
            get { return _entityTypeShape; }
        }
    }
}
