// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Resources;
    using System.Runtime.Serialization;

    /// <summary>
    /// Thrown when an error occurs committing a <see cref="DbTransaction"/>.
    /// </summary>
    [Serializable]
    public class CommitFailedException : DataException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CommitFailedException"/>
        /// </summary>
        public CommitFailedException()
            : base(Strings.CommitFailed)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of <see cref="CommitFailedException"/>
        /// </summary>
        /// <param name="message"> The exception message. </param>
        public CommitFailedException(string message)
            : base(message)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of <see cref="CommitFailedException"/>
        /// </summary>
        /// <param name="message"> The exception message. </param>
        /// <param name="innerException"> The inner exception. </param>
        public CommitFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitFailedException"/> class.
        /// </summary>
        /// <param name="info">The data necessary to serialize or deserialize an object.</param>
        /// <param name="context">Description of the source and destination of the specified serialized stream.</param>
        protected CommitFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
