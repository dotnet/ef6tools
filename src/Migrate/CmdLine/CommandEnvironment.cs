﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace CmdLine
{
    using System;

    internal class CommandEnvironment : ICommandEnvironment
    {
        public string CommandLine
        {
            get { return Environment.CommandLine; }
        }

        private string[] args;

        public string[] GetCommandLineArgs()
        {
            return args ?? (args = Environment.GetCommandLineArgs());
        }

        public string Program
        {
            get { return GetCommandLineArgs()[0]; }
        }
    }
}
