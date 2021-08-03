// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects
{
    internal struct StateManagerValue
    {
        public StateManagerMemberMetadata MemberMetadata;
        public object UserObject;
        public object OriginalValue;

        public StateManagerValue(StateManagerMemberMetadata metadata, object instance, object value)
        {
            MemberMetadata = metadata;
            UserObject = instance;
            OriginalValue = value;
        }
    }
}
