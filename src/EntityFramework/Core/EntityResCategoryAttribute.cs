﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core
{
    using System.ComponentModel;
    using System.Data.Entity.Resources;

    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
        | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field
        | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate
        | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter)]
    internal sealed class EntityResCategoryAttribute : CategoryAttribute
    {
        public EntityResCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return EntityRes.GetString(value);
        }
    }
}
