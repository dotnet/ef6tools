// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests.Model
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [ComplexType]
    public class RowDetails
    {
        public Guid rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
