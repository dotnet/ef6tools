// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.ViewModel
{
    internal partial class ScalarProperty
    {
        internal void ChangeEntityKey()
        {
            using (var t = Store.TransactionManager.BeginTransaction("Entity Key"))
            {
                EntityKey = !EntityKey;
                t.Commit();
            }
        }
    }
}
