// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace ConcurrencyModel
{
    using System.Collections.Generic;

    public class EngineSupplier
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public virtual ICollection<Engine> Engines { get; set; }
    }
}
