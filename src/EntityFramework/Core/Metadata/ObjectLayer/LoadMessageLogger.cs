﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Text;

    internal class LoadMessageLogger
    {
        private readonly Action<String> _logLoadMessage;
        private readonly Dictionary<EdmType, StringBuilder> _messages = new Dictionary<EdmType, StringBuilder>();

        internal LoadMessageLogger(Action<String> logLoadMessage)
        {
            _logLoadMessage = logLoadMessage;
        }

        internal virtual void LogLoadMessage(string message, EdmType relatedType)
        {
            if (_logLoadMessage != null)
            {
                _logLoadMessage(message);
            }

            LogMessagesWithTypeInfo(message, relatedType);
        }

        internal virtual string CreateErrorMessageWithTypeSpecificLoadLogs(string errorMessage, EdmType relatedType)
        {
            return new StringBuilder(errorMessage)
                .AppendLine(GetTypeRelatedLogMessage(relatedType)).ToString();
        }

        private string GetTypeRelatedLogMessage(EdmType relatedType)
        {
            DebugCheck.NotNull(relatedType);

            if (_messages.ContainsKey(relatedType))
            {
                return new StringBuilder()
                    .AppendLine()
                    .AppendLine(Strings.ExtraInfo)
                    .AppendLine(_messages[relatedType].ToString()).ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        private void LogMessagesWithTypeInfo(string message, EdmType relatedType)
        {
            DebugCheck.NotNull(relatedType);

            if (_messages.ContainsKey(relatedType))
            {
                // if this type already contains loading message, append the new message to the end
                _messages[relatedType].AppendLine(message);
            }
            else
            {
                _messages.Add(relatedType, new StringBuilder(message));
            }
        }
    }
}
