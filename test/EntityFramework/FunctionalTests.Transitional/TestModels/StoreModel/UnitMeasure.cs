// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests.Model
{
    using System.ComponentModel.DataAnnotations;

    public class UnitMeasure
    {
        public virtual string UnitMeasureCode { get; set; }

        [MaxLength(42)]
        public virtual string Name { get; set; }
    }
}
