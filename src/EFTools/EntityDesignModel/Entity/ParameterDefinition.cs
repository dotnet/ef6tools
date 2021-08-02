// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal class ParameterDefinition
    {
        internal string Name { get; set; }
        internal string Mode { get; set; }
        internal string Type { get; set; }

        internal ParameterDefinition()
        {
        }

        internal ParameterDefinition(Parameter parameter)
        {
            Name = parameter.Name.Value;
            Mode = parameter.Mode.Value;
            Type = parameter.Type.Value;
        }
    }
}
