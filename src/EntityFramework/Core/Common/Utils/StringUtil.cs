// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.Utils
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    // This class provides some useful string utilities, e.g., converting a
    // list to string.
    internal static class StringUtil
    {
        private const string s_defaultDelimiter = ", ";

        #region String Conversion - Unsorted

        // <summary>
        // Converts an enumeration of values to a delimited string list.
        // </summary>
        // <typeparam name="T"> Type of elements to convert. </typeparam>
        // <param name="values"> Values. If null, returns empty string. </param>
        // <param name="converter"> Converter. If null, uses default invariant culture converter. </param>
        // <param name="delimiter"> Delimiter. If null, uses default (', ') </param>
        // <returns> Delimited list of values in string. </returns>
        internal static string BuildDelimitedList<T>(IEnumerable<T> values, ToStringConverter<T> converter, string delimiter)
        {
            if (null == values)
            {
                return String.Empty;
            }
            if (null == converter)
            {
                converter = InvariantConvertToString;
            }
            if (null == delimiter)
            {
                delimiter = s_defaultDelimiter;
            }

            var sb = new StringBuilder();
            var first = true;
            foreach (var value in values)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(delimiter);
                }
                sb.Append(converter(value));
            }

            return sb.ToString();
        }

        // effects: Converts list to a string separated by a comma with
        // string.Empty used for null values
        internal static string ToCommaSeparatedString(IEnumerable list)
        {
            return ToSeparatedString(list, s_defaultDelimiter, string.Empty);
        }

        // effects: Converts list to a string separated by "separator" with
        // "nullValue" used for null values
        internal static string ToSeparatedString(IEnumerable list, string separator, string nullValue)
        {
            var builder = new StringBuilder();
            ToSeparatedString(builder, list, separator, nullValue);
            return builder.ToString();
        }

        #endregion

        #region String Conversion - Sorted

        // effects: Converts the list to a list of strings, sorts its
        // and then converts to a string separated by a comma with
        // string.Empty used for null values
        internal static string ToCommaSeparatedStringSorted(IEnumerable list)
        {
            return ToSeparatedStringSorted(list, s_defaultDelimiter, string.Empty);
        }

        // effects: Converts the list to a list of strings, sorts its using
        // StringComparer.Ordinal
        // and then converts to a string separated by  "separator" with
        // with "nullValue" used for null values
        internal static string ToSeparatedStringSorted(IEnumerable list, string separator, string nullValue)
        {
            var builder = new StringBuilder();
            ToSeparatedStringPrivate(builder, list, separator, nullValue, true);
            return builder.ToString();
        }

        #endregion

        #region StringBuilder routines

        internal static string MembersToCommaSeparatedString(IEnumerable members)
        {
            var builder = new StringBuilder();
            builder.Append("{");
            ToCommaSeparatedString(builder, members);
            builder.Append("}");
            return builder.ToString();
        }

        internal static void ToCommaSeparatedString(StringBuilder builder, IEnumerable list)
        {
            ToSeparatedStringPrivate(builder, list, s_defaultDelimiter, string.Empty, false);
        }

        internal static void ToCommaSeparatedStringSorted(StringBuilder builder, IEnumerable list)
        {
            ToSeparatedStringPrivate(builder, list, s_defaultDelimiter, string.Empty, true);
        }

        internal static void ToSeparatedString(StringBuilder builder, IEnumerable list, string separator)
        {
            ToSeparatedStringPrivate(builder, list, separator, string.Empty, false);
        }

        internal static void ToSeparatedStringSorted(StringBuilder builder, IEnumerable list, string separator)
        {
            ToSeparatedStringPrivate(builder, list, separator, string.Empty, true);
        }

        // effects: Modifies stringBuilder to contain a string of values from list
        // separated by "separator" with "nullValue" used for null values
        internal static void ToSeparatedString(
            StringBuilder stringBuilder, IEnumerable list, string separator,
            string nullValue)
        {
            ToSeparatedStringPrivate(stringBuilder, list, separator, nullValue, false);
        }

        // effects: Converts the list to a list of strings, sorts its (if
        // toSort is true) and then converts to a string separated by
        // "separator" with "nullValue" used for null values.
        private static void ToSeparatedStringPrivate(
            StringBuilder stringBuilder, IEnumerable list, string separator,
            string nullValue, bool toSort)
        {
            if (null == list)
            {
                return;
            }
            var isFirst = true;
            // Get the list of strings first
            var elementStrings = new List<string>();
            foreach (var element in list)
            {
                string str;
                // Get the element or its default null value
                if (element == null)
                {
                    str = nullValue;
                }
                else
                {
                    str = FormatInvariant("{0}", element);
                }
                elementStrings.Add(str);
            }

            if (toSort)
            {
                // Sort the list
                elementStrings.Sort(StringComparer.Ordinal);
            }

            // Now add the strings to the stringBuilder
            foreach (var str in elementStrings)
            {
                if (false == isFirst)
                {
                    stringBuilder.Append(separator);
                }
                stringBuilder.Append(str);
                isFirst = false;
            }
        }

        #endregion

        #region Some Helper routines

        internal static string FormatInvariant(string format, params object[] args)
        {
            Debug.Assert(args.Length > 0, "Formatting utilities must be called with at least one argument");
            return String.Format(CultureInfo.InvariantCulture, format, args);
        }

        // effects: Formats args according to the format string and adds it
        // to builder. Returns the modified builder
        internal static StringBuilder FormatStringBuilder(StringBuilder builder, string format, params object[] args)
        {
            Debug.Assert(args.Length > 0, "Formatting utilities must be called with at least one argument");
            builder.AppendFormat(CultureInfo.InvariantCulture, format, args);
            return builder;
        }

        // effects: Generates a new line and then indents the new line by
        // indent steps in builder -- indent steps are determined internally
        // by this method. Returns the modified builder
        internal static StringBuilder IndentNewLine(StringBuilder builder, int indent)
        {
            builder.AppendLine();
            for (var i = 0; i < indent; i++)
            {
                builder.Append("    ");
            }
            return builder;
        }

        // effects: returns a string of the form 'arrayVarName[index]'
        internal static string FormatIndex(string arrayVarName, int index)
        {
            var builder = new StringBuilder(arrayVarName.Length + 10 + 2);
            return builder.Append(arrayVarName).Append('[').Append(index).Append(']').ToString();
        }

        private static string InvariantConvertToString<T>(T value)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        #endregion

        #region Delegates

        internal delegate string ToStringConverter<T>(T value);

        #endregion
    }
}
