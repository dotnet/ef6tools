// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SimpleModel
{
    using System.Collections.Generic;

    public class Category
    {
        public Category()
        {
            Products = new List<Product>();
        }

        public Category(string id)
            : this()
        {
            Id = id;
        }

        public string Id { get; set; }
        public ICollection<Product> Products { get; set; }
        public string DetailedDescription { get; set; }
    }
}
