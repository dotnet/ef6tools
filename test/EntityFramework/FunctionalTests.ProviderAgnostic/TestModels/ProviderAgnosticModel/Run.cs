// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestModels.ProviderAgnosticModel
{
    using System.Collections.Generic;
    using System.Data.Entity.Spatial;

    public class Run
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Purpose { get; set; }
        public Owner RunOwner { get; set; }
        public ICollection<Task> Tasks { get; set; }
    }
}