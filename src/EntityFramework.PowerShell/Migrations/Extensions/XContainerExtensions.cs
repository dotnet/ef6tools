﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Extensions
{
    using System.Data.Entity.Utilities;
    using System.Xml.Linq;

    internal static class XContainerExtensions
    {
        public static XElement GetOrCreateElement(
            this XContainer container, string elementName, params XAttribute[] attributes)
        {
            DebugCheck.NotNull(container);
            DebugCheck.NotEmpty(elementName);

            var element = container.Element(elementName);
            if (element == null)
            {
                element = new XElement(elementName, attributes);
                container.Add(element);
            }
            return element;
        }
    }
}
