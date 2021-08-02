// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace CmdLine
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors",
        Justification = "SerializeObjectState used instead")]
    [Serializable]
    internal class CommandLineException : Exception
    {
        [NonSerialized]
        private CommandLineExceptionState _state;

        public CommandLineException(string message)
            : base(message)
        {
            SubscribeToSerializeObjectState();
        }

        public CommandLineException(CommandArgumentHelp argumentHelp)
            : base(CheckNotNull(argumentHelp).Message)
        {
            ArgumentHelp = argumentHelp;

            SubscribeToSerializeObjectState();
        }

        public CommandLineException(CommandArgumentHelp argumentHelp, Exception inner)
            : base(CheckNotNull(argumentHelp).Message, inner)
        {
            ArgumentHelp = argumentHelp;

            SubscribeToSerializeObjectState();
        }

        public CommandArgumentHelp ArgumentHelp
        {
            get { return _state.ArgumentHelp; }
            set { _state.ArgumentHelp = value; }
        }

        private static CommandArgumentHelp CheckNotNull(CommandArgumentHelp argumentHelp)
        {
            if (argumentHelp == null)
            {
                throw new ArgumentNullException("argumentHelp");
            }
            return argumentHelp;
        }

        private void SubscribeToSerializeObjectState()
        {
            SerializeObjectState += (_, a) => a.AddSerializedState(_state);
        }

        [Serializable]
        private struct CommandLineExceptionState : ISafeSerializationData
        {
            public CommandArgumentHelp ArgumentHelp { get; set; }

            public void CompleteDeserialization(object deserialized)
            {
                var commandLineException = (CommandLineException)deserialized;

                commandLineException._state = this;
                commandLineException.SubscribeToSerializeObjectState();
            }
        }
    }
}
