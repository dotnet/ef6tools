// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    using System;

    [Serializable]
    internal class EnumTypeMemberClipboardFormat : AnnotatableElementClipboardFormat
    {
        private string _memberName;
        private string _memberValue;

        public EnumTypeMemberClipboardFormat(EnumTypeMember enumTypeMember)
            : base(enumTypeMember)
        {
            _memberName = enumTypeMember.Name.Value;
            _memberValue = enumTypeMember.Value.Value;
        }

        internal string MemberValue
        {
            get { return _memberValue; }
            set { _memberValue = value; }
        }

        internal string MemberName
        {
            get { return _memberName; }
            set { _memberName = value; }
        }
    }
}
