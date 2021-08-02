// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder.Internal
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.Utils;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;

    // <summary>
    // Validates an input enumerable argument with a specific element type,
    // converting each input element into an instance of a specific output element type,
    // then producing a final result of another specific type.
    // </summary>
    // <typeparam name="TElementIn"> The element type of the input enumerable </typeparam>
    // <typeparam name="TElementOut"> The element type that input elements are converted to </typeparam>
    // <typeparam name="TResult"> The type of the final result </typeparam>
    internal sealed class EnumerableValidator<TElementIn, TElementOut, TResult>
    {
        private readonly string argumentName;
        private readonly IEnumerable<TElementIn> target;

        internal EnumerableValidator(IEnumerable<TElementIn> argument, string argumentName)
        {
            this.argumentName = argumentName;
            target = argument;
        }

        private int expectedElementCount = -1;

        // <summary>
        // Gets or sets a value that determines whether an exception is thrown if the enumerable argument is empty.
        // </summary>
        // <remarks>
        // AllowEmpty is ignored if <see cref="ExpectedElementCount" /> is set.
        // If ExpectedElementCount is set to zero, an empty collection will not cause an exception to be thrown,
        // even if AllowEmpty is set to <c>false</c>.
        // </remarks>
        public bool AllowEmpty { get; set; }

        // <summary>
        // Gets or set a value that determines the number of elements expected in the enumerable argument.
        // A value of <c>-1</c> indicates that any number of elements is permitted, including zero.
        // Use <see cref="AllowEmpty" /> to disallow an empty list when ExpectedElementCount is set to -1.
        // </summary>
        public int ExpectedElementCount
        {
            get { return expectedElementCount; }
            set { expectedElementCount = value; }
        }

        // <summary>
        // Gets or sets the function used to convert an element from the enumerable argument into an instance of
        // the desired output element type. The position of the input element is also specified as an argument to this function.
        // </summary>
        public Func<TElementIn, int, TElementOut> ConvertElement { get; set; }

        // <summary>
        // Gets or sets the function used to create the output collection from a list of converted enumerable elements.
        // </summary>
        public Func<List<TElementOut>, TResult> CreateResult { get; set; }

        // <summary>
        // Gets or sets an optional function that can retrieve the name of an element from the enumerable argument.
        // If this function is set, duplicate input element names will result in an exception. Null or empty names will
        // not result in an exception. If specified, this function will be called after <see cref="ConvertElement" />.
        // </summary>
        public Func<TElementIn, int, string> GetName { get; set; }

        // <summary>
        // Validates the input enumerable, converting each input element and producing the final instance of
        // <typeparamref
        //     name="TResult" />
        // as a result.
        // </summary>
        // <returns>
        // The instance of <typeparamref name="TResult" /> produced by calling the <see cref="CreateResult" /> function on the list of elements produced by calling the
        // <see
        //     cref="ConvertElement" />
        // function on each element of the input enumerable.
        // </returns>
        // <exception cref="ArgumentNullException">If the input enumerable itself is null</exception>
        // <exception cref="ArgumentNullException">
        // If
        // <typeparamref name="TElementIn" />
        // is a nullable type and any element of the input enumerable is null.
        // </exception>
        // <exception cref="ArgumentException">
        // If
        // <see cref="ExpectedElementCount" />
        // is set and the actual number of input elements is not equal to this value.
        // </exception>
        // <exception cref="ArgumentException">
        // If
        // <see cref="ExpectedElementCount" />
        // is -1,
        // <see cref="AllowEmpty" />
        // is set to
        // <c>false</c>
        // and the input enumerable is empty.
        // </exception>
        // <exception cref="ArgumentException">
        // If
        // <see cref="GetName" />
        // is set and a duplicate name is derived for more than one input element.
        // </exception>
        // <remarks>
        // Other exceptions may be thrown by the <see cref="ConvertElement" /> and <see cref="CreateResult" /> functions, and by the
        // <see
        //     cref="GetName" />
        // function, if specified.
        // </remarks>
        internal TResult Validate()
        {
            return Validate(
                target,
                argumentName,
                ExpectedElementCount,
                AllowEmpty,
                ConvertElement,
                CreateResult,
                GetName);
        }

        private static TResult Validate(
            IEnumerable<TElementIn> argument,
            string argumentName,
            int expectedElementCount,
            bool allowEmpty,
            Func<TElementIn, int, TElementOut> map,
            Func<List<TElementOut>, TResult> collect,
            Func<TElementIn, int, string> deriveName)
        {
            DebugCheck.NotNull(argument);

            DebugCheck.NotNull(map);
            DebugCheck.NotNull(collect);

            var checkNull = (default(TElementIn) == null);
            var checkCount = (expectedElementCount != -1);
            Dictionary<string, int> nameIndex = null;
            if (deriveName != null)
            {
                nameIndex = new Dictionary<string, int>();
            }

            var pos = 0;
            var validatedElements = new List<TElementOut>();
            foreach (var elementIn in argument)
            {
                // More elements in 'arguments' than expected?
                if (checkCount && pos == expectedElementCount)
                {
                    throw new ArgumentException(Strings.Cqt_ExpressionList_IncorrectElementCount, argumentName);
                }

                if (checkNull && elementIn == null)
                {
                    // Don't call FormatIndex unless an exception is actually being thrown
                    throw new ArgumentNullException(StringUtil.FormatIndex(argumentName, pos));
                }

                var elementOut = map(elementIn, pos);
                validatedElements.Add(elementOut);

                if (deriveName != null)
                {
                    var name = deriveName(elementIn, pos);
                    Debug.Assert(name != null, "GetName should not produce null");
                    var foundIndex = -1;
                    if (nameIndex.TryGetValue(name, out foundIndex))
                    {
                        throw new ArgumentException(
                            Strings.Cqt_Util_CheckListDuplicateName(foundIndex, pos, name), StringUtil.FormatIndex(argumentName, pos));
                    }
                    nameIndex[name] = pos;
                }

                pos++;
            }

            // If an expected count was specified, the actual count must match
            if (checkCount)
            {
                if (pos != expectedElementCount)
                {
                    throw new ArgumentException(Strings.Cqt_ExpressionList_IncorrectElementCount, argumentName);
                }
            }
            else
            {
                // No expected count was specified, simply verify empty vs. non-empty.
                if (0 == pos
                    && !allowEmpty)
                {
                    throw new ArgumentException(Strings.Cqt_Util_CheckListEmptyInvalid, argumentName);
                }
            }

            return collect(validatedElements);
        }
    }
}
