// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests.Model
{
    using System;

    public class CustomerDiscount
    {
        public virtual int CustomerID { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual decimal Discount { get; set; }

        public virtual Guid rowguid { get; set; }

        public virtual DateTime ModifiedDate { get; set; }
    }
}
