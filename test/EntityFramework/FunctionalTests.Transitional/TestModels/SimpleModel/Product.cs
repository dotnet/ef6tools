// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SimpleModel
{
    public class Product : ProductBase
    {
        public string CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
