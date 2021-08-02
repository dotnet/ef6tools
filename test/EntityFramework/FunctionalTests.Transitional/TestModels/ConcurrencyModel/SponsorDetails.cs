﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace ConcurrencyModel
{
    using System.ComponentModel.DataAnnotations.Schema;

    [ComplexType]
    public class SponsorDetails
    {
        public int Days { get; set; }
        public decimal Space { get; set; }
    }
}
