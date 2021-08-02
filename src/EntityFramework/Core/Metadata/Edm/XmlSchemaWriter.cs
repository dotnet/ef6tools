// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Text;
    using System.Xml;

    internal abstract class XmlSchemaWriter
    {
        protected XmlWriter _xmlWriter;
        protected double _version;

        internal void WriteComment(string comment)
        {
            if (!String.IsNullOrEmpty(comment))
            {
                _xmlWriter.WriteComment(comment);
            }
        }

        internal virtual void WriteEndElement()
        {
            _xmlWriter.WriteEndElement();
        }

        protected static string GetQualifiedTypeName(string prefix, string typeName)
        {
            var sb = new StringBuilder();
            return sb.Append(prefix).Append(".").Append(typeName).ToString();
        }

        internal static string GetLowerCaseStringFromBoolValue(bool value)
        {
            return value ? XmlConstants.True : XmlConstants.False;
        }
    }
}
