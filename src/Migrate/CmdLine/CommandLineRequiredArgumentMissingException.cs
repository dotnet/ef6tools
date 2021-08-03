// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace CmdLine
{
    using System;
    using System.Data.Entity.Migrations.Console.Resources;

    [Serializable]
    internal class CommandLineRequiredArgumentMissingException : CommandLineException
    {
        public CommandLineRequiredArgumentMissingException(Type argumentType, string argumentName, int parameterIndex)
            : base(new CommandArgumentHelp(argumentType, FormatMessage(argumentName, parameterIndex)))
        {
        }

        private static string FormatMessage(string argumentName, int parameterIndex)
        {
            return parameterIndex == -1
                       ? Strings.MissingCommandLineParameter("", argumentName, CommandLine.Text)
                       : Strings.MissingCommandLineParameter(parameterIndex, argumentName, string.Join(" ", CommandLine.Args));
        }
    }
}
