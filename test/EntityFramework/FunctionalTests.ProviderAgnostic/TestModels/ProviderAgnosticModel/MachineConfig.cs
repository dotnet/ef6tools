﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestModels.ProviderAgnosticModel
{
    using System.Data.Entity.Spatial;

    public class MachineConfig : Config
    {
        public string Host { get; set; }
        public Guid Address { get; set; }
    }
}
