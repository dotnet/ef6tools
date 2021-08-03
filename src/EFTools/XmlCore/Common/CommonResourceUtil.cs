// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Common
{
    using Microsoft.Data.Tools.XmlDesignerBase.Common;

    internal static class CommonResourceUtil
    {
        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string GetString(string format, params object[] args)
        {
            return string.Format(CommonResource.Culture, format, args);
        }
    }
}
