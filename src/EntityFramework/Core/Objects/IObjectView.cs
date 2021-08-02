// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects
{
    using System.ComponentModel;

    internal interface IObjectView
    {
        void EntityPropertyChanged(object sender, PropertyChangedEventArgs e);
        void CollectionChanged(object sender, CollectionChangeEventArgs e);
    }
}
