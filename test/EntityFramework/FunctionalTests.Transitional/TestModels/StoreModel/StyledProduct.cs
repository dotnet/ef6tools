// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests.Model
{
    using System.ComponentModel.DataAnnotations;

    public class StyledProduct : Product
    {
        [StringLength(150)]
        public virtual string Style { get; set; }
    }
}
