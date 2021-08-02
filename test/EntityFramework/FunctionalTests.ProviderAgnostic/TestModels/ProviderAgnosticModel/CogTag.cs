// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestModels.ProviderAgnosticModel
{

    public class CogTag
    {
        public Guid Id { get; set; }
        public string Note { get; set; }

        public virtual Gear Gear { get; set; }
    }
}
