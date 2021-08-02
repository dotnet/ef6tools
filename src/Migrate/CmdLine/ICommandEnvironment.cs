// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace CmdLine
{
    internal interface ICommandEnvironment
    {
        string CommandLine { get; }

        string[] GetCommandLineArgs();

        string Program { get; }
    }
}
