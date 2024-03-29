﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestHelpers
{
    using System.Globalization;

    public static class LocalizationTestHelpers
    {
        public static bool IsEnglishLocale()
        {
            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en";
        }
    }
}
