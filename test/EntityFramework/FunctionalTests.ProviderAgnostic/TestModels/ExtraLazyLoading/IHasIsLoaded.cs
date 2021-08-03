// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestModels.ExtraLazyLoading
{
    public interface IHasIsLoaded
    {
        bool IsLoaded { get; set; }
    }
}
