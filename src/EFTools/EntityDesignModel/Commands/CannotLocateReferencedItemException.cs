﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [Serializable]
    internal class CannotLocateReferencedItemException : Exception
    {
        internal CannotLocateReferencedItemException()
        {
        }

        protected CannotLocateReferencedItemException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
