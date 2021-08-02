// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests.Model
{
    using System.Collections.Generic;

    public class ProductDescription
    {
        public virtual int ProductDescriptionID { get; set; }
        public virtual string Description { get; set; }

        public virtual RowDetails RowDetails { get; set; }

        public virtual ICollection<ProductModelProductDescriptionCulture> ProductModelProductDescriptionCultures { get; set; }
    }
}
