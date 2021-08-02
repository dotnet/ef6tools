// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests.Model
{
    using System;

    public class DiscontinuedProduct : Product
    {
        public virtual DateTime DiscontinuedDate { get; set; }
    }
}
