﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Utilities
{
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;

    internal static class DbContextExtensions
    {
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static XDocument GetModel(this DbContext context)
        {
            DebugCheck.NotNull(context);

            return GetModel(w => EdmxWriter.WriteEdmx(context, w));
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static XDocument GetModel(Action<XmlWriter> writeXml)
        {
            DebugCheck.NotNull(writeXml);

            using (var memoryStream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(
                    memoryStream, new XmlWriterSettings
                                      {
                                          Indent = true
                                      }))
                {
                    writeXml(xmlWriter);
                }

                memoryStream.Position = 0;

                return XDocument.Load(memoryStream);
            }
        }
    }
}
