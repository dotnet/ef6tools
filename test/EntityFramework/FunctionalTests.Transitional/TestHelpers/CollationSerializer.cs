// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestHelpers
{
    using System.Data.Entity.Infrastructure;

    public class CollationSerializer : IMetadataAnnotationSerializer
    {
        public string Serialize(string name, object value)
        {
            return ((CollationAttribute)value).CollationName;
        }

        public object Deserialize(string name, string value)
        {
            return new CollationAttribute(value);
        }
    }
}
