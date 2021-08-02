// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.Internal.Materialization
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using System.Linq.Expressions;

    // <summary>
    // What we return from our materialization of a collection column must be
    // exactly the type that the compilers expected when they generated the
    // code that asked for it.  This class wraps our enumerators and derives
    // from all the possible options, covering all the bases.
    // </summary>
    internal class CompensatingCollection<TElement> : IOrderedQueryable<TElement>, IOrderedEnumerable<TElement>
    {
        #region private state

        // <summary>
        // The thing we're compensating for
        // </summary>
        private readonly IEnumerable<TElement> _source;

        // <summary>
        // An expression that returns the source as a constant
        // </summary>
        private readonly Expression _expression;

        #endregion

        #region constructors

        public CompensatingCollection(IEnumerable<TElement> source)
        {
            DebugCheck.NotNull(source);

            _source = source;
            _expression = Expression.Constant(source);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        #endregion

        #region IEnumerable<TElement> Members

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        #endregion

        #region IOrderedEnumerable<TElement> Members

        IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<K>(
            Func<TElement, K> keySelector, IComparer<K> comparer, bool descending)
        {
            throw new NotSupportedException(Strings.ELinq_CreateOrderedEnumerableNotSupported);
        }

        #endregion

        #region IQueryable Members

        Type IQueryable.ElementType
        {
            get { return typeof(TElement); }
        }

        Expression IQueryable.Expression
        {
            get { return _expression; }
        }

        IQueryProvider IQueryable.Provider
        {
            get { throw new NotSupportedException(Strings.ELinq_UnsupportedQueryableMethod); }
        }

        #endregion

        #region IQueryable<TElement> Members

        #endregion
    }
}
