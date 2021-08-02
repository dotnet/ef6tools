﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestModels.ProviderAgnosticModel
{
    public abstract class Weapon
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        
        // 1 - 1 self reference
        public virtual Weapon SynergyWith { get; set; }
    }
}
